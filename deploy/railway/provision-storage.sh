#!/bin/sh
# Creates ArMenu's two buckets and their CORS rules on the Railway storage service.
#
# Buckets and CORS are infrastructure, provisioned before the API runs — the API only creates them for itself in
# Development (see deploy/compose.production.yml, where a storage-setup container does exactly this). Run once from
# the repository root, after the storage service has a public domain:
#
#   STORAGE_URL=https://armenu-storage-production.up.railway.app \
#   DASHBOARD_URL=https://armenu-dashboard-production.up.railway.app \
#   AWS_ACCESS_KEY_ID='…' AWS_SECRET_ACCESS_KEY='…' deploy/railway/provision-storage.sh
#
# The keys are the STORAGE_ACCESS_KEY / STORAGE_SECRET_KEY of the storage service.
#
# Re-running is safe, and is what you do after changing the dashboard's domain: the uploads bucket's CORS rule names
# that exact origin, and browsers refuse the presigned PUT without it.
set -eu

: "${STORAGE_URL:?set STORAGE_URL to the storage service's public URL}"
: "${DASHBOARD_URL:?set DASHBOARD_URL to the dashboard's public URL, the origin that uploads}"
: "${AWS_ACCESS_KEY_ID:?set AWS_ACCESS_KEY_ID to the storage access key}"
: "${AWS_SECRET_ACCESS_KEY:?set AWS_SECRET_ACCESS_KEY to the storage secret key}"

AWS_IMAGE="${AWS_IMAGE:-amazon/aws-cli:2.33.0}"
export AWS_DEFAULT_REGION="${AWS_DEFAULT_REGION:-us-east-1}"

s3() {
  docker run --rm \
    -e AWS_ACCESS_KEY_ID -e AWS_SECRET_ACCESS_KEY -e AWS_DEFAULT_REGION \
    "$AWS_IMAGE" --endpoint-url "$STORAGE_URL" "$@"
}

for bucket in armenu-uploads armenu-assets; do
  if s3 s3api head-bucket --bucket "$bucket" 2>/dev/null; then
    echo "==> $bucket already exists"
  else
    echo "==> Creating $bucket"
    s3 s3api create-bucket --bucket "$bucket"
  fi
done

# Only the dashboard may PUT: a presigned URL is a capability, and the origin check is what stops another site from
# spending one it somehow obtained.
echo "==> CORS on armenu-uploads for $DASHBOARD_URL"
s3 s3api put-bucket-cors --bucket armenu-uploads --cors-configuration "{
  \"CORSRules\": [
    {
      \"AllowedMethods\": [\"PUT\"],
      \"AllowedOrigins\": [\"$DASHBOARD_URL\"],
      \"AllowedHeaders\": [\"*\"],
      \"MaxAgeSeconds\": 600
    }
  ]
}"

# Published assets are read by every guest's browser, from whatever origin the menu is served on, and by three.js
# fetching a model, which needs CORS even for a plain GET.
echo "==> CORS on armenu-assets"
s3 s3api put-bucket-cors --bucket armenu-assets --cors-configuration '{
  "CORSRules": [
    {
      "AllowedMethods": ["GET", "HEAD"],
      "AllowedOrigins": ["*"],
      "AllowedHeaders": ["*"],
      "MaxAgeSeconds": 3600
    }
  ]
}'

echo "==> Storage provisioned."
