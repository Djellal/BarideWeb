#!/bin/bash

cd /opt/baridufas/BarideWeb/
git pull
dotnet publish
sudo service baridufas stop
cp -r /opt/baridufas/BarideWeb/bin/Release/net10.0/publish/ /opt/baridufas/
sudo service baridufas start
