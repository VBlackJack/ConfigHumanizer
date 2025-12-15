#!/bin/sh
# Version 0.1

VERSION="0.1"

PATH=/usr/local/sbin:/usr/local/bin:/sbin:/bin:/usr/sbin:/usr/bin:/usr/local/isaac/bin:/usr/local/security/bin

DIR_TMP="/DATA"

echo ----- Delete files and directories at `date +%F.%H%M%S` ---- Version : $VERSION ------

echo ----- Cleaning older than 1 days /DATA ...
cd /DATA/
find . -mtime +1 -exec /bin/rm -rf {} \; 2>/dev/null

echo ----- End of Delete tmp files at `date +%F.%H%M%S` ----

exit 0

