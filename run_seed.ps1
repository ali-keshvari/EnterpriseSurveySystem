$sqlPath = "d:\Antigravity\EnterpriseSurveySystem\seed_tree_data.sql"
$sqlText = [System.IO.File]::ReadAllText($sqlPath, [System.Text.Encoding]::UTF8)

$conn = New-Object System.Data.SqlClient.SqlConnection("Server=.;Database=HrEnterpriseDb;Integrated Security=True;")
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = $sqlText
$cmd.CommandTimeout = 60
$rows = $cmd.ExecuteNonQuery()
$conn.Close()

Write-Host "Seeding completed successfully! Affected rows: $rows"
