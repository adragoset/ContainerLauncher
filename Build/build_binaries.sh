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

docker build -t devopsintralox/containerlauncher:${SHA1}_build -f Build/Dockerfile --network=${NET} .
docker create --name launcher_binaries devopsintralox/containerlauncher:${SHA1}_build
mkdir pkg
docker cp launcher_binaries:/launcher_binaries.tar.gz pkg/launcher_binaries.tar.gz