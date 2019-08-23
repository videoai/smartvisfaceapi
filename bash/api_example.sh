#!/usr/bin/env bash

# The credentials
EMAIL=demo@digitalbarriers.com
PASSWORD=Demo123123
AUTH_SERVER=http://192.168.86.38:10000
CLIENT_ID=48b009b1e9e651d806e91cc24a4239cdc28cabaafee51635f8b575f309db2d88
CLIENT_SECRET=2ae6ac4addaac04f688e6c6c17f6bcad501ace832f6da64e2d027f6d6322c8f6

# Some variables that do not change
OAUTH_BODY_HASH="2jmj7l5rSw0yVb%2FvlWAYkK%2FYBwk%3D"
OAUTH_VERSION="1.0"
OAUTH_SIGNATURE_METHOD="HMAC-SHA1"
OAUTH_TOKEN="a_single_token"

function rawurlencode() {
	local LANG=C
	for ((i=0;i<${#1};i++)); do
		if [[ ${1:$i:1} =~ ^[a-zA-Z0-9\.\~\_\-]$ ]]; then
			printf "${1:$i:1}"
		else
			printf '%%%02X' "'${1:$i:1}"
		fi
	done
}

function hash_hmac {
  data="$1"
  key="$2"
  echo -n "$data" | openssl dgst -binary -sha1 -hmac "$key" | openssl base64
}

OAuth_nonce () {
	# Return a nonce
	md5sum <<< "$RANDOM-$(date +%s.%N)" | cut -d' ' -f 1
}

OAuth_timestamp () {
	# Return timestamp
	echo "$(date +%s)"
}

do_login() {
    curl -X POST -H "Authorization: ${AUTHORIZATION}" -d "email=${EMAIL}&password=${PASSWORD}" ${AUTH_SERVER}/auth/api_login
}

list_subjects() {
    echo ${DEVICE_DATA}
    curl -X GET -H "Device: ${DEVICE_DATA}" -H "Authorization: ${AUTHORIZATION}"  ${API_URL}/subject
}

# First we need to authenticate ourselves
OAUTH_TIMESTAMP=$(OAuth_timestamp)
OAUTH_NONCE=$(OAuth_nonce)
METHOD="POST"
END_POINT=$(rawurlencode "/auth/api_login")


# Sort out Parameters (in alphabetical order)
OAUTH="oauth_body_hash=${OAUTH_BODY_HASH}&oauth_consumer_key=${CLIENT_ID}&oauth_nonce=${OAUTH_NONCE}&oauth_signature_method=${OAUTH_SIGNATURE_METHOD}&oauth_timestamp=${OAUTH_TIMESTAMP}&oauth_token=${OAUTH_TOKEN}&oauth_version=${OAUTH_VERSION}"
EMAIL=$(rawurlencode ${EMAIL})
PASSWORD=$(rawurlencode ${PASSWORD})
NORMALIZED_PARAMETERS=$(rawurlencode "email=${EMAIL}&${OAUTH}&password=${PASSWORD}")
BASE_STRING="${METHOD}&${END_POINT}&${NORMALIZED_PARAMETERS}"
SIGNED=$(hash_hmac "${BASE_STRING}" "${CLIENT_SECRET}&")
SIGNED=$(rawurlencode ${SIGNED})
echo ${SIGNED}

# Sort out Authorization header
AUTHORIZATION="OAuth realm=\"\", oauth_body_hash=\"${OAUTH_BODY_HASH}\", oauth_nonce=\"${OAUTH_NONCE}\", oauth_timestamp=\"${OAUTH_TIMESTAMP}\", oauth_consumer_key=\"${CLIENT_ID}\", oauth_signature_method=\"${OAUTH_SIGNATURE_METHOD}\", oauth_version=\"${OAUTH_VERSION}\", oauth_token=\"${OAUTH_TOKEN}\", oauth_signature=\"${SIGNED}\""
JSON=$(do_login)
TOKEN=`echo ${JSON} | jq -r '.oauth_token.token'`
echo ${TOKEN}
API_URL=`echo ${JSON} | jq -r '.user.api_url'`
echo ${API_URL}

# Now we have token lets perform a request

# The Device
DEVICE_ID=myDeviceId
DEVICE_NAME=myDeviceName
DEVICE_LAT=38.1499
DEVICE_LNG=144.3617
DEVICE_DATA="device_id=\"${DEVICE_ID}\", device_name=\"${DEVICE_NAME}\", lat=\"${DEVICE_LAT}\", lng=\"${DEVICE_LNG}\""
DEVICE_DATA_ENCODED=$(rawurlencode "${DEVICE_DATA}")

METHOD="GET"
END_POINT=$(rawurlencode "/subject")
OAUTH_TIMESTAMP=$(OAuth_timestamp)
OAUTH_NONCE=$(OAuth_nonce)
OAUTH="oauth_body_hash=${OAUTH_BODY_HASH}&oauth_consumer_key=${CLIENT_ID}&oauth_nonce=${OAUTH_NONCE}&oauth_signature_method=${OAUTH_SIGNATURE_METHOD}&oauth_timestamp=${OAUTH_TIMESTAMP}&oauth_token=${TOKEN}&oauth_version=${OAUTH_VERSION}"
NORMALIZED_PARAMETERS=$(rawurlencode "device_data=${DEVICE_DATA_ENCODED}&${OAUTH}")
BASE_STRING="${METHOD}&${END_POINT}&${NORMALIZED_PARAMETERS}"
SIGNED=$(hash_hmac "${BASE_STRING}" "${CLIENT_SECRET}&")
SIGNED=$(rawurlencode ${SIGNED})
AUTHORIZATION="OAuth realm=\"\", oauth_body_hash=\"${OAUTH_BODY_HASH}\", oauth_nonce=\"${OAUTH_NONCE}\", oauth_timestamp=\"${OAUTH_TIMESTAMP}\", oauth_consumer_key=\"${CLIENT_ID}\", oauth_signature_method=\"${OAUTH_SIGNATURE_METHOD}\", oauth_version=\"${OAUTH_VERSION}\", oauth_token=\"${TOKEN}\", oauth_signature=\"${SIGNED}\""
JSON=$(list_subjects)
echo ${JSON}













