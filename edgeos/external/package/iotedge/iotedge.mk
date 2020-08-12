IOTEDGE_VERSION = 1.0.9.4
IOTEDGE_SITE = git://github.com/Azure/iotedge.git
IOTEDGE_GIT_SUBMODULES = YES
IOTEDGE_LICENSE = MIT
IOTEDGE_LICENSE_FILES = LICENSE-MIT

IOTEDGE_DEPENDENCIES = host-rustc
IOTEDGE_CARGO_ENV = CARGO_HOME=$(HOST_DIR)/share/cargo IOTEDGE_HOST=unix:///var/lib/iotedge/mgmt.sock

# Note: add LIBIOTHSM_NOBUILD=true to IOTEDGE_CARGO_ENV to use an HSM built elsewhere.
#   See https://github.com/Azure/iotedge/blob/1.0.9.4/edgelet/hsm-sys/build.rs#L197
#   Patch 0001-suppress-error.patch should no longer be needed in this case.

IOTEDGE_CARGO_MODE = $(if $(BR2_ENABLE_DEBUG),debug,release)

IOTEDGE_BIN_DIR = edgelet/target/$(RUSTC_TARGET_NAME)/$(IOTEDGE_CARGO_MODE)

IOTEDGE_CARGO_OPTS = \
    -p iotedged -p iotedge \
	--$(IOTEDGE_CARGO_MODE) \
	--target=$(RUSTC_TARGET_NAME) \
	--manifest-path=$(@D)/edgelet/Cargo.toml

define IOTEDGE_BUILD_CMDS
	$(TARGET_MAKE_ENV) $(IOTEDGE_CARGO_ENV) cargo build $(IOTEDGE_CARGO_OPTS)
endef

define IOTEDGE_INSTALL_TARGET_CMDS

	# iotedge and iotedged
	$(INSTALL) -D -m 755 $(@D)/$(IOTEDGE_BIN_DIR)/iotedged $(TARGET_DIR)/usr/bin/iotedged
	$(INSTALL) -D -m 755 $(@D)/$(IOTEDGE_BIN_DIR)/iotedge $(TARGET_DIR)/usr/bin/iotedge

	# libiothsm.so
	$(INSTALL) -D -m 755 $(@D)/$(IOTEDGE_BIN_DIR)/build/hsm-sys-*/out/lib/libiothsm.so.1.0.8 $(TARGET_DIR)/usr/lib/libiothsm.so.1.0.8
	ln -sf libiothsm.so.1.0.8 $(TARGET_DIR)/usr/lib/libiothsm.so
	ln -sf libiothsm.so.1.0.8 $(TARGET_DIR)/usr/lib/libiothsm.so.1

	# config.yaml
	$(INSTALL) -D -m 400 $(IOTEDGE_PKGDIR)/config.yaml $(TARGET_DIR)/etc/iotedge/config.yaml

	# S70iotedged
	$(INSTALL) -D -m 755 $(IOTEDGE_PKGDIR)/S70iotedged $(TARGET_DIR)/etc/init.d/S70iotedged

endef

$(eval $(generic-package))