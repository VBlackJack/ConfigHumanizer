#!/bin/sh

# detect_file.sh
#	Detect the file in the directory $DIR
#	Detect the File Creation and a Close_write
#	The close_write is for the size :
#		If we check the size in the close "CREATE" and the file is big then size isn't correct ==> we should wait the close
#	PROJECT : Define the project for which we are checking (today ANSSI, P4S)
#	We exclude swp files. Their are created if we modify a file within the monitor directory
#

PROJECT="ANSSI"
DIR="/DATA"
EVENTS=" create,close_write"
EXCLUDE1="\.(swp|swpx)" 

if [ "$1" == "-h" ]; then
  echo "Usage : $0 <directory> (the directory to be survey)"
  exit 0
fi

if [ $# -eq 1 ]; then
  	DIR=$1
	echo "DIR=$DIR"
fi

inotifywait -m -r -e $EVENTS --timefmt '%Y-%m-%d %H:%M:%S' --exclude $EXCLUDE1 --format '%T %e %w%f' "${DIR}" | while read date time event NEWFILE
do
	if [ -e ${NEWFILE} ]
	then
        	DAS=`ls -l ${NEWFILE} | cut -d ' ' -f 3`
        	SIZE=`ls -l ${NEWFILE} | cut -d ' ' -f 5`
		SHASUM=`sha256sum ${NEWFILE} | cut -d ' ' -f 1`
		if [ $event = "CREATE" ] 
		then
			logger -t FILE_EXCHANGE "${PROJECT}: [$date $time] Action: $event ; User: $DAS ; Filename: ${NEWFILE} ; SHA-256: ${SHASUM}"
			#echo "FILE_EXCHANGE: ${PROJECT}: [$date $time] Action: $event ; User: $DAS ; From: $IP ; Filename: ${NEWFILE}"
		else
			logger -t FILE_EXCHANGE "${PROJECT}: [$date $time] Action: $event ; User: $DAS ; Filename: ${NEWFILE}; Size: $SIZE ; SHA-256: ${SHASUM}"
			#echo "FILE_EXCHANGE: ${PROJECT}: [$date $time] Action: $event ; User: $DAS ; From: $IP ; Filename: ${NEWFILE}; Size: $SIZE"
		fi
	fi
done

