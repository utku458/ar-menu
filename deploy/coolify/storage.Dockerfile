# syntax=docker/dockerfile:1
# ArMenu's object storage on Coolify: the SeaweedFS image of docker-compose.yml, with its identity file written at
# start-up instead of bind-mounted (see storage-entrypoint.sh).
#
# Coolify application settings:
#   Base Directory       /
#   Dockerfile Location  /deploy/coolify/storage.Dockerfile
#   Ports Exposes        8333
#   Persistent Storage   volume mounted at /data
#
# Build from the repository root:
#   docker build -f deploy/coolify/storage.Dockerfile -t armenu-storage .
FROM chrislusf/seaweedfs:4.47

COPY deploy/coolify/storage-entrypoint.sh /usr/local/bin/armenu-storage
RUN chmod +x /usr/local/bin/armenu-storage

# The base image's entrypoint dispatches on the first argument; this one always starts the same server.
ENTRYPOINT ["/usr/local/bin/armenu-storage"]
CMD []
