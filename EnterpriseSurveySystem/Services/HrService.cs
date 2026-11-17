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
            var list = new List<DepartmentDto>();
            if (string.IsNullOrWhiteSpace(_connectionString)) return list;

            const string sql = @"
                SELECT DepartmentId, DepNameFa
                FROM [prj].[Department]
                WHERE IsActive = 1
                ORDER BY DepNameFa ASC;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new DepartmentDto
                        {
                            DepartmentId = reader.GetInt32(0),
                            DepNameFa = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
                        });
                    }
                }
            }
            return list;
        }

        public List<PositionDto> GetActivePositions(List<int> departmentIds = null)
        {
            var list = new List<PositionDto>();
            if (string.IsNullOrWhiteSpace(_connectionString)) return list;

            string sql = @"
                SELECT PositionId, PositionNameFa, DepartmentId
                FROM [prj].[Position]
                WHERE IsActive = 1";

            if (departmentIds != null && departmentIds.Count > 0)
            {
                var idList = string.Join(",", departmentIds.Select(id => id.ToString()));
                sql += $" AND DepartmentId IN ({idList})";
            }

            sql += " ORDER BY PositionNameFa ASC;";

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
                            DepartmentId = reader.GetInt32(2)
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

            string sql = @"
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
                WHERE p.IsActive = 1";

            if (departmentIds != null && departmentIds.Count > 0)
            {
                var depList = string.Join(",", departmentIds.Select(id => id.ToString()));
                sql += $" AND d.DepartmentId IN ({depList})";
            }

            if (positionIds != null && positionIds.Count > 0)
            {
                var posList = string.Join(",", positionIds.Select(id => id.ToString()));
                sql += $" AND pos.PositionId IN ({posList})";
            }

            sql += " ORDER BY p.LastName ASC, p.FirstName ASC;";

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
