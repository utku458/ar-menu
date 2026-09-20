#!/bin/sh
# Renders SeaweedFS's identity file, then hands over to the server.
#
# deploy/storage/s3.json is committed with development credentials and mounted by Docker Compose. Neither is possible
# here: a Railway service has no bind mounts, and the credentials are real. The identities are therefore written at
# start-up from the service's own variables, so the only place the secret exists is Railway's variable store.
#
# The identity list is otherwise the same as the Compose one: one account the API signs with, and anonymous read on
# the assets bucket alone, which is what lets a guest's browser fetch a poster or a model without a signature while
# the uploads bucket stays private.
set -eu

: "${STORAGE_ACCESS_KEY:?set STORAGE_ACCESS_KEY to the access key the API signs with}"
: "${STORAGE_SECRET_KEY:?set STORAGE_SECRET_KEY to the matching secret key}"

# Railway injects PORT; the S3 endpoint is the only port this service publishes.
PORT="${PORT:-8333}"
DATA_DIR="${STORAGE_DATA_DIR:-/data}"

mkdir -p /etc/seaweedfs "$DATA_DIR"

cat > /etc/seaweedfs/s3.json <<JSON
{
  "identities": [
    {
      "name": "armenu-api",
      "credentials": [{ "accessKey": "${STORAGE_ACCESS_KEY}", "secretKey": "${STORAGE_SECRET_KEY}" }],
      "actions": ["Admin", "Read", "Write", "List", "Tagging"]
    },
    {
      "name": "anonymous",
      "actions": ["Read:armenu-assets"]
    }
  ]
}
JSON

exec weed server \
  -s3 \
  -s3.config=/etc/seaweedfs/s3.json \
  -s3.port="$PORT" \
  -dir="$DATA_DIR" \
  -master.volumeSizeLimitMB="${STORAGE_VOLUME_SIZE_LIMIT_MB:-1024}" \
  -volume.max=0
