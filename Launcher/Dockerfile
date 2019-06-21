FROM debian:buster-slim

ENV CONFIG_PATH=/local \
ROOT_CONFIG_PATH=/local \
DEST_CONFIG_PATH=/app_launcher \
MOUNT_CONFIG_PATH=/local

COPY Build/launcher_binaries.tar.gz /launcher_binaries.tar.gz

RUN apt-get update && \
apt-get install dos2unix && \
mkdir /temp && \
mkdir /app && \
mkdir /app_launcher

ADD Launcher/run.sh /app/run.sh

WORKDIR /
RUN tar -xvzf /launcher_binaries.tar.gz && \
rm /launcher_binaries.tar.gz && \
rm -rf /app/arm64 && \
mv /app/x64/* /app && \
rm -rf /app/x64

RUN dos2unix /app/run.sh && \
chmod +x /app/run.sh && \
chmod +x /app/Launcher

#Run the app
ENTRYPOINT [ "/app/run.sh" ]