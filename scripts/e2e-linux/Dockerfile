# Headless OrcaSlicer-family test bench: Xvfb + Mesa software GL + a tiny window manager.
# The slicer only needs to *run* here, not render fast, so no GPU is involved.
FROM ubuntu:24.04

ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update -qq && apt-get install -y -qq --no-install-recommends \
        ca-certificates curl \
        xvfb openbox xdotool x11-utils imagemagick \
        mesa-utils libgl1-mesa-dri libegl1 libglu1-mesa \
        libgtk-3-0 libwebkit2gtk-4.1-0 libsoup-3.0-0 \
        libgstreamer1.0-0 libgstreamer-plugins-base1.0-0 libgstreamer-gl1.0-0 \
        fonts-dejavu-core locales \
    && rm -rf /var/lib/apt/lists/* \
    && localedef -i en_US -f UTF-8 en_US.UTF-8

ENV DISPLAY=:99 \
    LANG=en_US.UTF-8 \
    LC_ALL=en_US.UTF-8 \
    LIBGL_ALWAYS_SOFTWARE=1 \
    GALLIUM_DRIVER=llvmpipe \
    WEBKIT_DISABLE_COMPOSITING_MODE=1

COPY e2e.sh /e2e.sh
RUN chmod +x /e2e.sh
ENTRYPOINT ["/e2e.sh"]
