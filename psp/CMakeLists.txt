cmake_minimum_required(VERSION 3.11)
project(psp-connect C)

add_executable(${PROJECT_NAME}
    src/main.c
    src/ui.c
    src/usb.c
)

target_include_directories(${PROJECT_NAME} PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/src
    ${CMAKE_CURRENT_SOURCE_DIR}/../protocol
)

target_compile_definitions(${PROJECT_NAME} PRIVATE PSPCONNECT_MOCK_USB=0)

target_link_libraries(${PROJECT_NAME} PRIVATE
    pspdebug
    pspdisplay
    pspge
    pspctrl
    pspusb
    pspusb_driver
    pspusbbus_driver
    pspsdk
    pspkernel
)

create_pbp_file(
    TARGET ${PROJECT_NAME}
    ICON_PATH NULL
    BACKGROUND_PATH NULL
    PREVIEW_PATH NULL
    TITLE "psp-connect"
    VERSION 01.00
)
