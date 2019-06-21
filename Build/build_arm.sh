#!/bin/bash

while getopts "n:s:b:t:" opt; do
  case $opt in
    n) NET="$OPTARG"
    ;;
    s) SHA1="$OPTARG"
    ;;
    b) BRANCH="$OPTARG"
    ;;
    t) TAG="$OPTARG"
    ;;
    \?) echo "Invalid option -$OPTARG" >&2
    ;;
  esac
done

docker build -t devopsintralox/containerLauncher:${SHA1}_arm -f Launcher/Dockerfile_arm --network=${NET} .
docker push devopsintralox/containerLauncher:${SHA1}_arm
if [ "${TAG}" != "" ]; then
    docker tag devopsintralox/containerLauncher:${SHA1}_arm devopsintralox/containerLauncher:${TAG}_arm
    docker push devopsintralox/containerLauncher:${TAG}_arm
else
    docker tag devopsintralox/containerLauncher:${SHA1}_arm devopsintralox/containerLauncher:${BRANCH}_arm
    docker push devopsintralox/containerLauncher:${BRANCH}_arm
fi