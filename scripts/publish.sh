#!/usr/bin/env bash

sudo apt-get update
sudo apt-get install python3-pip
sudo pip3 install awscli

mkdir -p ~/.aws
cat > ~/.aws/credentials << EOL
[default]
aws_access_key_id = ${AWS_ACCESS_KEY_ID}
aws_secret_access_key = ${AWS_SECRET_ACCESS_KEY}
EOL

cd NosCore.TravisLambda/NosCore.Travis
dotnet lambda package
cd ./bin/Release/netcoreapp2.1/
aws lambda update-function-code --function-name noscore-travis --zip-file fileb://NosCore.Travis.zip > /dev/null;
