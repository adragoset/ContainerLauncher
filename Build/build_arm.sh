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

docker build -t devopsintralox/containerlauncher:${SHA1}_arm -f Launcher/Dockerfile_arm --network=${NET} .
docker push devopsintralox/containerlauncher:${SHA1}_arm
if [ "${TAG}" != "" ]; then
    docker tag devopsintralox/containerlauncher:${SHA1}_arm devopsintralox/containerlauncher:${TAG}_arm
    docker push devopsintralox/containerlauncher:${TAG}_arm
else
    docker tag devopsintralox/containerlauncher:${SHA1}_arm devopsintralox/containerlauncher:${BRANCH}_arm
    docker push devopsintralox/containerlauncher:${BRANCH}_arm
fi