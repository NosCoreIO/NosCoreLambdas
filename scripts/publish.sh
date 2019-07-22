#!/usr/bin/env bash
sudo pip install --user awscli
eval $(aws ecr get-login --region us-west-2 --no-include-email)

cd NosCore.Travis
dotnet lambda package
cd ./bin/Release/netcoreapp2.1/

mkdir -p ~/.aws

cat > ~/.aws/credentials << EOL
[default]
aws_access_key_id = ${AWS_ACCESS_KEY_ID}
aws_secret_access_key = ${AWS_SECRET_ACCESS_KEY}
EOL

aws lambda update-function-code --function-name noscore-travis --zip-file fileb://NosCore.Travis.zip
