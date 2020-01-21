#!/usr/bin/env bash

mkdir -p ~/.aws
cat > ~/.aws/credentials << EOL
[default]
aws_access_key_id = ${AWS_ACCESS_KEY_ID}
aws_secret_access_key = ${AWS_SECRET_ACCESS_KEY}
EOL

#lambda travis publish
cd NosCore.TravisLambda/NosCore.Travis
dotnet lambda package
cd ./bin/Release/netcoreapp2.1/
aws lambda update-function-code --function-name noscore-travis --zip-file fileb://NosCore.Travis.zip > /dev/null;

#serverless noscore-donation
cd NosCore.DonationLambda/NosCore.Donation
sam package --template-file ../../../serverless.template --s3-bucket noscore-donation --output-template-file serverless.yaml
sam deploy --template-file serverless.yaml --stack-name noscore-donation --capabilities CAPABILITY_IAM