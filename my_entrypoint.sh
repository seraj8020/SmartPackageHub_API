#!/bin/sh
# Add your custom logic here (e.g., waiting for database)
echo "Running custom entrypoint logic..."
# Then, execute kc.sh to start Keycloak
# exec /opt/keycloak/bin/kc.sh start-dev
# ENTRYPOINT ["/opt/keycloak/bin/kc.sh"]
# CMD ["start-dev", "--optimized"]
/opt/keycloak/bin/kcadm.sh config credentials --server http://localhost:8080/health/ready --realm master --user admin --password admin
/opt/keycloak/bin/kcadm.sh get http://localhost:9000/${KC_HTTP_RELATIVE_PATH}/health

Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Solution Items", "Solution Items", "{8EC462FD-D22E-90A8-E5CE-7E832BA40C5D}"
	ProjectSection(SolutionItems) = preProject
		KeyCloakDockerfile\Dockerfile = KeyCloakDockerfile\Dockerfile
	EndProjectSection
EndProject