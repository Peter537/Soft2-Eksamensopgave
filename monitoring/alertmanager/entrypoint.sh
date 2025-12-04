#!/bin/sh
# Entrypoint script for Alertmanager
# Substitutes environment variables in the config template

set -e

TEMPLATE_FILE="/etc/alertmanager/alertmanager.yml.template"
CONFIG_FILE="/etc/alertmanager/alertmanager.yml"

# Check if Discord webhook is configured
if [ -z "$DISCORD_WEBHOOK_ALERT" ]; then
    echo "WARNING: DISCORD_WEBHOOK_ALERT is not set. Alerts will not be sent to Discord."
    echo "Set DISCORD_WEBHOOK_ALERT in your .env file to enable Discord notifications."
    # Use a dummy URL that will fail gracefully
    DISCORD_WEBHOOK_ALERT="https://discord.com/api/webhooks/not-configured/please-set-DISCORD_WEBHOOK_ALERT"
fi

# Substitute environment variables using sed (envsubst not available in alpine)
sed "s|\${DISCORD_WEBHOOK_ALERT}|${DISCORD_WEBHOOK_ALERT}|g" "$TEMPLATE_FILE" > "$CONFIG_FILE"

echo "Alertmanager configuration generated from template"

# Start Alertmanager
exec /bin/alertmanager \
    --config.file="$CONFIG_FILE" \
    --storage.path=/alertmanager \
    --web.external-url=http://localhost:9093 \
    "$@"
