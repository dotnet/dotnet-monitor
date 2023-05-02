include(${CLR_ENG_NATIVE_DIR}/configurecompiler.cmake)

if (MSVC)
  # Debug build specific flags
  add_linker_flag(/INCREMENTAL:NO DEBUG) # prevent "warning LNK4075: ignoring '/INCREMENTAL' due to '/OPT:REF' specification"
  add_linker_flag(/OPT:REF DEBUG)
  add_linker_flag(/OPT:NOICF DEBUG)
endif(MSVC)
