using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using EnterpriseSurveySystem.Models.ViewModels;

namespace EnterpriseSurveySystem.Services
{
    public class HrService : IHrService
    {
        private readonly string _connectionString;

        public HrService()
        {
            var connSetting = ConfigurationManager.ConnectionStrings["HrDbConnection"];
            _connectionString = connSetting != null ? connSetting.ConnectionString : string.Empty;
        }

        public HrService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<DepartmentDto> GetActiveDepartments()
        {
            var flatList = new List<DepartmentDto>();
            if (string.IsNullOrWhiteSpace(_connectionString)) return flatList;

            const string sql = @"
                SELECT DepartmentId, DepHdrId, DepNameFa, DepLevel
                FROM [prj].[Department]
                WHERE IsActive = 1
                ORDER BY DepLevel ASC, DepHdrId ASC, DepNameFa ASC;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        flatList.Add(new DepartmentDto
                        {
                            DepartmentId = reader.GetInt32(0),
                            DepHdrId = reader.GetInt32(1),
                            DepNameFa = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            DepLevel = reader.GetInt32(3)
                        });
                    }
                }
            }

            // Determine which departments have children
            var parentIds = new HashSet<int>(flatList.Select(d => d.DepHdrId).Where(id => id > 0));
            foreach (var d in flatList)
            {
                d.HasChildren = parentIds.Contains(d.DepartmentId);
            }

            // Build pre-order depth-first hierarchical list so parent is immediately followed by its children
            var orderedList = BuildHierarchicalList(flatList);
            return orderedList;
        }

        private List<DepartmentDto> BuildHierarchicalList(List<DepartmentDto> all)
        {
            var result = new List<DepartmentDto>();
            var lookup = all.ToLookup(d => d.DepHdrId);

            var allIds = new HashSet<int>(all.Select(x => x.DepartmentId));
            // Root nodes: DepHdrId == 0 or DepHdrId not in all
            var roots = all.Where(d => d.DepHdrId == 0 || !allIds.Contains(d.DepHdrId))
                           .OrderBy(d => d.DepartmentId)
                           .ToList();

            void Traverse(DepartmentDto node)
            {
                result.Add(node);
                var children = lookup[node.DepartmentId].OrderBy(c => c.DepartmentId).ToList();
                node.Children = children;
                foreach (var child in children)
                {
                    Traverse(child);
                }
            }

            foreach (var root in roots)
            {
                Traverse(root);
            }

            // Fallback for detached nodes if any
            if (result.Count < all.Count)
            {
                var addedIds = new HashSet<int>(result.Select(r => r.DepartmentId));
                foreach (var remaining in all.Where(d => !addedIds.Contains(d.DepartmentId)))
                {
                    result.Add(remaining);
                }
            }

            return result;
        }

        public List<PositionDto> GetActivePositions(List<int> departmentIds = null)
        {
            var list = new List<PositionDto>();
            if (string.IsNullOrWhiteSpace(_connectionString)) return list;

            string sql;
            if (departmentIds != null && departmentIds.Count > 0)
            {
                var idList = string.Join(",", departmentIds.Select(id => id.ToString()));
                sql = $@"
                    WITH DeptHierarchy AS (
                        SELECT DepartmentId, DepHdrId
                        FROM [prj].[Department]
                        WHERE IsActive = 1 AND DepartmentId IN ({idList})
                        UNION ALL
                        SELECT d.DepartmentId, d.DepHdrId
                        FROM [prj].[Department] d
                        INNER JOIN DeptHierarchy dh ON d.DepHdrId = dh.DepartmentId
                        WHERE d.IsActive = 1
                    )
                    SELECT pos.PositionId, pos.PositionNameFa, pos.DepartmentId, d.DepNameFa
                    FROM [prj].[Position] pos
                    INNER JOIN [prj].[Department] d ON pos.DepartmentId = d.DepartmentId
                    WHERE pos.IsActive = 1 
                      AND pos.DepartmentId IN (SELECT DepartmentId FROM DeptHierarchy)
                    ORDER BY pos.PositionNameFa ASC;";
            }
            else
            {
                sql = @"
                    SELECT pos.PositionId, pos.PositionNameFa, pos.DepartmentId, d.DepNameFa
                    FROM [prj].[Position] pos
                    INNER JOIN [prj].[Department] d ON pos.DepartmentId = d.DepartmentId
                    WHERE pos.IsActive = 1
                    ORDER BY pos.PositionNameFa ASC;";
            }

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new PositionDto
                        {
                            PositionId = reader.GetInt32(0),
                            PositionNameFa = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            DepartmentId = reader.GetInt32(2),
                            DepartmentName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
                        });
                    }
                }
            }
            return list;
        }

        public List<PersonDto> GetActivePersons(List<int> departmentIds = null, List<int> positionIds = null)
        {
            var list = new List<PersonDto>();
            if (string.IsNullOrWhiteSpace(_connectionString)) return list;

            string cte = "";
            string whereFilter = "WHERE p.IsActive = 1";

            if (departmentIds != null && departmentIds.Count > 0)
            {
                var depList = string.Join(",", departmentIds.Select(id => id.ToString()));
                cte = $@"
                    WITH DeptHierarchy AS (
                        SELECT DepartmentId, DepHdrId
                        FROM [prj].[Department]
                        WHERE IsActive = 1 AND DepartmentId IN ({depList})
                        UNION ALL
                        SELECT d.DepartmentId, d.DepHdrId
                        FROM [prj].[Department] d
                        INNER JOIN DeptHierarchy dh ON d.DepHdrId = dh.DepartmentId
                        WHERE d.IsActive = 1
                    ) ";
                whereFilter += " AND d.DepartmentId IN (SELECT DepartmentId FROM DeptHierarchy)";
            }

            if (positionIds != null && positionIds.Count > 0)
            {
                var posList = string.Join(",", positionIds.Select(id => id.ToString()));
                whereFilter += $" AND pos.PositionId IN ({posList})";
            }

            string sql = cte + $@"
                SELECT 
                    p.PersonId, 
                    p.FirstName, 
                    p.LastName, 
                    p.FullName, 
                    p.PersonelCode, 
                    pos.PositionId, 
                    pos.PositionNameFa, 
                    d.DepartmentId, 
                    d.DepNameFa,
                    p.moblie,
                    p.Email
                FROM [prj].[Person] p
                INNER JOIN [prj].[PersonPosition] pp ON p.PersonId = pp.PersonId AND pp.IsLast = 1
                INNER JOIN [prj].[Position] pos ON pp.PositionId = pos.PositionId AND pos.IsActive = 1
                INNER JOIN [prj].[Department] d ON pos.DepartmentId = d.DepartmentId AND d.IsActive = 1
                {whereFilter}
                ORDER BY p.LastName ASC, p.FirstName ASC;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new PersonDto
                        {
                            PersonId = reader.GetInt32(0),
                            FirstName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            LastName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            FullName = reader.IsDBNull(3) ? (reader.GetString(1) + " " + reader.GetString(2)) : reader.GetString(3),
                            PersonelCode = reader.GetInt32(4),
                            PositionId = reader.GetInt32(5),
                            PositionName = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                            DepartmentId = reader.GetInt32(7),
                            DepartmentName = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                            Mobile = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                            Email = reader.IsDBNull(10) ? string.Empty : reader.GetString(10)
                        });
                    }
                }
            }
            return list;
        }

        public List<PersonDto> GetPersonsByIds(List<int> personIds)
        {
            var list = new List<PersonDto>();
            if (string.IsNullOrWhiteSpace(_connectionString) || personIds == null || personIds.Count == 0) return list;

            var idList = string.Join(",", personIds.Select(id => id.ToString()));
            string sql = $@"
                SELECT 
                    p.PersonId, 
                    p.FirstName, 
                    p.LastName, 
                    p.FullName, 
                    p.PersonelCode, 
                    pos.PositionId, 
                    pos.PositionNameFa, 
                    d.DepartmentId, 
                    d.DepNameFa,
                    p.moblie,
                    p.Email
                FROM [prj].[Person] p
                INNER JOIN [prj].[PersonPosition] pp ON p.PersonId = pp.PersonId AND pp.IsLast = 1
                INNER JOIN [prj].[Position] pos ON pp.PositionId = pos.PositionId AND pos.IsActive = 1
                INNER JOIN [prj].[Department] d ON pos.DepartmentId = d.DepartmentId AND d.IsActive = 1
                WHERE p.IsActive = 1 AND p.PersonId IN ({idList})
                ORDER BY p.LastName ASC, p.FirstName ASC;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new PersonDto
                        {
                            PersonId = reader.GetInt32(0),
                            FirstName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            LastName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            FullName = reader.IsDBNull(3) ? (reader.GetString(1) + " " + reader.GetString(2)) : reader.GetString(3),
                            PersonelCode = reader.GetInt32(4),
                            PositionId = reader.GetInt32(5),
                            PositionName = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                            DepartmentId = reader.GetInt32(7),
                            DepartmentName = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                            Mobile = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                            Email = reader.IsDBNull(10) ? string.Empty : reader.GetString(10)
                        });
                    }
                }
            }
            return list;
        }
    }
}
