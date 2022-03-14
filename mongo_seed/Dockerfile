FROM mongo

ENV DATABASE_URI mongodb://mongo:27017
ENV DATABASE_NAME stalingrad0
COPY *.json .
COPY seed.sh .
RUN chmod +x seed.sh
CMD ["bash", "-c", "./seed.sh"]


