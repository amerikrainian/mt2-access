#!/bin/bash

dotnet nuget update source --username $GITHUB_USER --password $GH_AUTH_TOKEN monster-train-packages --store-password-in-clear-text
dotnet build -c Release --output ./dist