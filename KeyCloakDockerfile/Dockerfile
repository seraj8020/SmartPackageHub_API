###### Using As Base Image ###### # Stage 1: Build Keycloak extensions or configurations
FROM quay.io/keycloak/keycloak:26.1.2  AS builder

# for demonstration purposes only, please make sure to use proper certificates in production instead
# Example: Copy custom theme or provider into the builder stage
# COPY my-custom-theme /opt/keycloak/themes/my-custom-theme

WORKDIR /opt/keycloak
RUN keytool -genkeypair -storepass password -storetype PKCS12 -keyalg RSA -keysize 2048 -dname "CN=server" -alias server -ext "SAN:c=DNS:keycloak,IP:127.0.0.1" -keystore conf/server.keystore
RUN /opt/keycloak/bin/kc.sh build
###### Non-Interactive Installations #########


######### Update & Upgrade the Image ##########
# Stage 2: Create the final runtime image using Ubuntu

FROM debian:latest
RUN apt-get update && apt-get install -y curl
###### Use The ENTRYPOINT From The Original Images Dockerfile. ######
###### The Original Dockerfile https://quay.io/repository/keycloak/keycloak/manifest/sha256:122ff5badfb88afe1027bc8e1a9c85532148ee813bee30c01173947a445d4fe7 ######
FROM quay.io/keycloak/keycloak:26.1.2
# Copy the built artifacts from the builder stage
# Example: Copy a custom Keycloak theme or provider JAR
COPY --from=builder /opt/keycloak/ /opt/keycloak/

# Set up your application within the Ubuntu image
# For example, if you were setting up a custom application that interacts with Keycloak

ENTRYPOINT ["/opt/keycloak/bin/kc.sh"]
CMD ["start-dev"]
