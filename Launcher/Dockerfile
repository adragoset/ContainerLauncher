FROM debian:stretch-slim

ENV CONFIG_PATH=/local \
ROOT_CONFIG_PATH=/local \
DEST_CONFIG_PATH=/app_launcher \
MOUNT_CONFIG_PATH=/local

COPY Build/launcher_binaries.tar.gz /launcher_binaries.tar.gz

RUN apt-get update && \
dpkg --remove docker docker-engine docker.io && \
apt-get install -y dos2unix libc6 libgcc1 libgssapi-krb5-2 libicu57 liblttng-ust0 libssl1.0.2 libstdc++6 zlib1g apt-transport-https ca-certificates curl gnupg2 software-properties-common && \
mkdir /temp && \
mkdir /app && \
mkdir /app_launcher && \
curl -fsSL https://download.docker.com/linux/debian/gpg | apt-key add - && \
add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/debian $(lsb_release -cs) stable" && \
apt-get update && \
apt-get install -y docker-ce

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

EXPOSE 5000

#Run the app
ENTRYPOINT [ "/app/run.sh" ]