#!/bin/bash
 
DATA_DIR=/DATA
QUARANTINE=/tmp/quarantine
LOG=/var/log/clamavd-tr.log
 
while :
do
 
inotifywait -q -r -e  create,modify,move "$DATA_DIR" --format '%w%f|%e' | sed --unbuffered 's/|.*//g' |
 
while read FILE; do 
	logger -t clamd "Scan of file '$FILE'."
	clamdscan -m -v --fdpass "$FILE" --move=$QUARANTINE
        if [ "$?" == "1" ]; then
		logger -t clamd "Malware found in the file '$FILE'. This file has been moved to $QUARANTINE"
        fi
done
done
