FROM postgres:18

COPY init /docker-entrypoint-initdb.d

RUN chmod +x /docker-entrypoint-initdb.d/*.sh