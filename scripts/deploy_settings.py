"""Configuracao exclusiva do deploy de Cobranca e de seu provedor mock."""

import secrets

IMAGE_BUILDS = {
    "api_image": ("oficina-mecanica-cobranca", "Dockerfile"),
    "mock_image": ("oficina-mecanica-pagamento-mock", "tools/OficinaMecanica.PagamentoMock/Dockerfile"),
}


def new_private_secrets():
    return {"database_password": secrets.token_hex(24), "database_root_password": secrets.token_hex(24),
            "payment_client_secret": secrets.token_hex(32), "payment_token": secrets.token_hex(32)}


def service_values(common, private):
    return {
        "database_secrets": {"MYSQL_DATABASE": "cobranca", "MYSQL_USER": "cobranca_app",
                             "MYSQL_PASSWORD": private["database_password"],
                             "MYSQL_ROOT_PASSWORD": private["database_root_password"]},
        "api_secrets": {
            "JwtSettings__SecretKey": common["jwt_secret"], "AdminUserSettings__Username": "admin",
            "AdminUserSettings__Password": common["admin_password"],
            "OrdensAtendimento__SecretKey": common["ordens_cobranca_secret"],
            "PagamentoMock__ClientId": "oficina-local", "PagamentoMock__ClientSecret": private["payment_client_secret"],
            "ConnectionStrings__DefaultConnection": "Server=mysql;Database=cobranca;User=cobranca_app;Password="
                + private["database_password"] + ";SslMode=None",
        },
        "mock_secrets": {"Mock__ClientId": "oficina-local", "Mock__ClientSecret": private["payment_client_secret"],
                         "Mock__Token": private["payment_token"]},
    }
