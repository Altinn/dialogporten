@export()
func dialogDbConnectionString(host string) string => empty(host) || contains(host, ';')
  ? fail('EntraToken requires dbHost to contain the PostgreSQL server FQDN, without connection-string options.')
  : 'Host=${host};Database=dialogporten;SSL Mode=VerifyFull'
