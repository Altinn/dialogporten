# Database Connection Forwarding

This utility helps forward PostgreSQL and Redis connections through SSH for Dialogporten environments.

## Prerequisites

- Azure CLI installed and configured
- Appropriate Azure account access
- Bash shell environment

## Usage

### Interactive Mode

Run the script without arguments for interactive mode:
```bash
./forward.sh
```

### Command-line Arguments

You can also specify the environment, database type, and local port directly:
```bash
./forward.sh -e test -t postgres -p 5433  # Use custom local port 5433
./forward.sh -e prod -t redis -p 6380     # Use custom local port 6380
```

Available options:
- `-e`: Environment (test, yt01, staging, prod)
- `-t`: Database type (postgres, redis)
- `-p`: Local port to bind on localhost (defaults to standard port for selected database)
- `-h`: Show help message

Default ports:
- PostgreSQL: 5432
- Redis: 6379

## Connecting to Databases

### PostgreSQL

1. Start the forwarding tool:
```bash
./forward.sh -e test -t postgres        # Uses default port 5432
./forward.sh -e test -t postgres -p 5433  # Uses custom port 5433
```
2. Once the tunnel is established, you can connect using:
   - Host: localhost
   - Port: 5432 (or your custom port if specified with -p)
   - Database: dialogporten
   - Username: shown in the connection string
   - Password: retrieve from Azure Key Vault

Example using psql:
```bash
psql "host=localhost port=5432 dbname=dialogporten user=<username>"
```

Example using pgAdmin:
- Host: localhost
- Port: 5432 (or your custom port)
- Database: dialogporten
- Username: (from connection string)
- Password: (from Key Vault)

### Redis

1. Start the forwarding tool:
```bash
./forward.sh -e test -t redis         # Uses default port 6379
./forward.sh -e test -t redis -p 6380 # Uses custom port 6380
```
2. Once the tunnel is established, you can connect using:
   - Host: localhost
   - Port: 6379 (or your custom port if specified with -p)
   - Password: shown in the connection string

Example using redis-cli:
```bash
redis-cli -h localhost -p 6379 -a "<password>"
```

Example connection string for applications:
```plaintext
redis://:<password>@localhost:6379  # Using default port
redis://:<password>@localhost:6380  # Using custom port
```

## Troubleshooting

- If you get authentication errors, ensure you're logged into the correct Azure account:
  - For test/yt01 environments, use the test subscription
  - For staging/prod environments, use the production subscription
- If the tunnel fails to establish, try running `az login` again
- Make sure you have the necessary permissions in the Azure subscription
- If the script fails to execute, ensure it has execute permissions:
  ```bash
  chmod +x forward.sh
  ```