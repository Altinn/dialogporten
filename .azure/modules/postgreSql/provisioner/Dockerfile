# Image for the database provisioning job. It runs inside the container app environment because
# the PostgreSQL server is VNet-injected and is not reachable from anywhere outside it.
#
# The base image is used for its psql client only; the PostgreSQL server entrypoint is replaced.
FROM postgres:18-alpine@sha256:d3e1620b530c944afa6e887d22eb899824da68e19c52024bf98f5220c88a65b2

# bash for the entrypoint, jq to read the workload list and the identity endpoint response, and
# curl to call that endpoint.
RUN apk add --no-cache bash curl jq

WORKDIR /opt/provisioner

# The SQL is kept with the rest of the PostgreSQL module so it stays next to the infrastructure it
# provisions, and is copied in here rather than duplicated.
COPY .azure/modules/postgreSql/sql/ ./sql/
COPY .azure/modules/postgreSql/provisioner/entrypoint.sh ./entrypoint.sh

RUN chmod +x ./entrypoint.sh

# The base image ships an unprivileged postgres user. The job only reads its own files and talks
# to the server, so it needs nothing beyond that.
USER postgres

ENTRYPOINT ["/opt/provisioner/entrypoint.sh"]
