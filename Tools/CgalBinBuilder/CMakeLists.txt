cmake_minimum_required(VERSION 3.24)

if(POLICY CMP0167)
    cmake_policy(SET CMP0167 NEW)
endif()

project(CgalBinBuilder LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

find_package(CGAL CONFIG REQUIRED)
find_package(Boost REQUIRED)

add_executable(CgalBinBuilder
    src/main.cpp
)

target_link_libraries(CgalBinBuilder
    PRIVATE
        CGAL::CGAL
        Boost::boost
)

if (MSVC)
    target_compile_options(CgalBinBuilder PRIVATE /W4 /permissive-)
else()
    target_compile_options(CgalBinBuilder PRIVATE -Wall -Wextra -Wpedantic)
endif()
