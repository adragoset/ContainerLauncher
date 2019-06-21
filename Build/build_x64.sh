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

mkdir pkg/
buildkite-agent artifact download pkg/launcher_binaries.tar.gz pkg/ --step ":hammer: Build Binaries .net"
cp pkg/launcher_binaries.tar.gz Build/launcher_binaries.tar.gz

docker build -t devopsintralox/containerlauncher:${SHA1} -f Launcher/Dockerfile --network=${NET} .
docker push devopsintralox/containerlauncher:${SHA1}
if [ "${TAG}" != "" ]; then
    docker tag devopsintralox/containerlauncher:${SHA1} devopsintralox/containerlauncher:${TAG}
    docker push devopsintralox/containerlauncher:${TAG} 
else
    docker tag devopsintralox/containerlauncher:${SHA1} devopsintralox/containerlauncher:${BRANCH}
    docker push devopsintralox/containerlauncher:${BRANCH}
fi