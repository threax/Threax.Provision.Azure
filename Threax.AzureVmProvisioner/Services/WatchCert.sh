#!/bin/bash

SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" >/dev/null 2>&1 && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ $SOURCE != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
scriptPath="$( cd -P "$( dirname "$SOURCE" )" >/dev/null 2>&1 && pwd )"

certinfo="REPLACE_OUT_FILE"
logFile="$scriptPath/watch-log.txt"
url="_acme-challenge.$CERTBOT_DOMAIN"
echo "{ \"url\": \"$url\", \"validation\": \"$CERTBOT_VALIDATION\" }" | tee $certinfo

validate="\"$CERTBOT_VALIDATION\""
value=""
until [ "$value" == "$validate" ]
do
  value=$(dig -t txt @8.8.8.8 $url +short)
  echo "Got $value expected $validate." | tee -a $logFile
  sleep 15s
done