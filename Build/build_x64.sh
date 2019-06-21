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

docker build -t devopsintralox/containerLauncher:${SHA1} -f Launcher/Dockerfile --network=${NET} .
docker push devopsintralox/containerLauncher:${SHA1}
if [ "${TAG}" != "" ]; then
    docker tag devopsintralox/containerLauncher:${SHA1} devopsintralox/containerLauncher:${TAG}
    docker push devopsintralox/containerLauncher:${TAG} 
else
    docker tag devopsintralox/containerLauncher:${SHA1} devopsintralox/containerLauncher:${BRANCH}
    docker push devopsintralox/containerLauncher:${BRANCH}
fi