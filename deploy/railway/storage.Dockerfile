# syntax=docker/dockerfile:1
# ArMenu's object storage on Railway: the SeaweedFS image of docker-compose.yml, with its identity file written at
# start-up instead of bind-mounted (see storage-entrypoint.sh).
#
# Railway service settings:
#   Root Directory       /
#   RAILWAY_DOCKERFILE_PATH  deploy/railway/storage.Dockerfile
#   Volume mounted at    /data
#
# Build from the repository root:
#   docker build -f deploy/railway/storage.Dockerfile -t armenu-storage .
FROM chrislusf/seaweedfs:4.47

COPY deploy/railway/storage-entrypoint.sh /usr/local/bin/armenu-storage
RUN chmod +x /usr/local/bin/armenu-storage

# The base image's entrypoint dispatches on the first argument; this one always starts the same server.
ENTRYPOINT ["/usr/local/bin/armenu-storage"]
CMD []
