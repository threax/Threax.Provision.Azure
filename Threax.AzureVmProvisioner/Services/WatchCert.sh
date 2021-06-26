#!/bin/bash

outFile="REPLACE_OUT_FILE"
url="_acme-challenge.$CERTBOT_DOMAIN"
echo "Validating domain '$CERTBOT_DOMAIN' for value '$CERTBOT_VALIDATION' checking TXT record at '$url'." | tee $outFile

validate="\"$CERTBOT_VALIDATION\""
value=""
until [ "$value" == "$validate" ]
do
  value=$(dig -t txt @8.8.8.8 $url +short)
  echo "Got $value expected $validate." | tee -a $outFile
  sleep 15s
done