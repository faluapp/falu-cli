FROM alpine

# Enable detection of running in a container
ENV DOTNET_RUNNING_IN_CONTAINER=true

RUN apk update && \
    apk upgrade && \
    apk add --no-cache ca-certificates gcompat icu-data-full icu-libs libgcc libssl3 libstdc++ tzdata zlib

COPY falu /bin/falu
ENTRYPOINT [ "/bin/falu" ]
