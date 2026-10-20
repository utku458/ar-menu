#!/usr/bin/env bash
# Asks Coolify to build and deploy, then waits until every deployment it
# started has finished, so a failed build on the server turns this job red
# instead of passing silently.
#
# Required environment:
#   COOLIFY_DEPLOY_URL  https://<coolify>/api/v1/deploy?uuid=<app>[,<app>...]
#   COOLIFY_TOKEN       Coolify API token with "deploy" and "read" permissions
# Optional:
#   COOLIFY_DEPLOY_TIMEOUT  seconds to wait for all deployments (default 1800)
#   COOLIFY_POLL_INTERVAL   seconds between status checks (default 10)

set -euo pipefail

readonly POLL_INTERVAL_SECONDS="${COOLIFY_POLL_INTERVAL:-10}"
readonly TIMEOUT_SECONDS="${COOLIFY_DEPLOY_TIMEOUT:-1800}"

if [ -z "${COOLIFY_DEPLOY_URL:-}" ] || [ -z "${COOLIFY_TOKEN:-}" ]; then
  echo "::error::COOLIFY_DEPLOY_URL or COOLIFY_TOKEN is not set in repository secrets."
  exit 1
fi

readonly API_BASE="${COOLIFY_DEPLOY_URL%%/api/v1/*}/api/v1"

coolify() {
  curl --silent --show-error --fail-with-body \
       --retry 3 --retry-connrefused --max-time 30 \
       --header "Authorization: Bearer ${COOLIFY_TOKEN}" \
       --header "Accept: application/json" \
       "$@"
}

# POST, not GET: Coolify maps `GET /api/v1/deploy` to a "POST required"
# handler that answers 405 even with a valid token.
response=$(coolify --request POST "${COOLIFY_DEPLOY_URL}") || {
  echo "::error::Coolify rejected the deploy request: ${response:-no response}"
  exit 1
}

deployments=()
while IFS= read -r uuid; do
  deployments+=("${uuid}")
done < <(jq -r '.deployments[]?.deployment_uuid // empty' <<<"${response}")
if [ "${#deployments[@]}" -eq 0 ]; then
  echo "::error::Coolify started no deployment: ${response}"
  exit 1
fi

echo "Coolify queued ${#deployments[@]} deployment(s): ${deployments[*]}"

deadline=$(( $(date +%s) + TIMEOUT_SECONDS ))
failed=0

for uuid in "${deployments[@]}"; do
  while :; do
    status=$(coolify "${API_BASE}/deployments/${uuid}" | jq -r '.status // "unknown"') || status="unreachable"

    case "${status}" in
      finished)
        echo "✓ ${uuid} finished"
        break
        ;;
      failed | cancelled-by-user)
        echo "::error::Deployment ${uuid} ended as '${status}'. See its log in the Coolify panel."
        failed=1
        break
        ;;
    esac

    if [ "$(date +%s)" -ge "${deadline}" ]; then
      echo "::error::Deployment ${uuid} still '${status}' after ${TIMEOUT_SECONDS}s."
      exit 1
    fi

    echo "… ${uuid}: ${status}"
    sleep "${POLL_INTERVAL_SECONDS}"
  done
done

exit "${failed}"
