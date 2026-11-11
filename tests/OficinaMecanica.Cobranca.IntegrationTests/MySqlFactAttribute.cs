namespace OficinaMecanica.Cobranca.IntegrationTests;

public sealed class MySqlFactAttribute : FactAttribute
{
    public MySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MYSQL_TEST_CONNECTION_STRING")))
            Skip = "Configure MYSQL_TEST_CONNECTION_STRING para verificar a migracao de dados legados.";
    }
}
