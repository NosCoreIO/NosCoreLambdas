#!/usr/bin/env bash

test -d ~/.linuxbrew && eval $(~/.linuxbrew/bin/brew shellenv)	
test -d /home/linuxbrew/.linuxbrew && eval $(/home/linuxbrew/.linuxbrew/bin/brew shellenv)	
test -r ~/.bash_profile && echo "eval \$($(brew --prefix)/bin/brew shellenv)" >>~/.bash_profile	
echo "eval \$($(brew --prefix)/bin/brew shellenv)" >>~/.profile	
brew --version	
brew tap aws/tap	
brew install aws-sam-cli	
sam --version

mkdir -p ~/.aws
cat > ~/.aws/credentials << EOL
[default]
aws_access_key_id = ${AWS_ACCESS_KEY_ID}
aws_secret_access_key = ${AWS_SECRET_ACCESS_KEY}
EOL

#lambda travis publish
cd NosCore.TravisLambda/NosCore.Travis
dotnet lambda package
cd ./bin/Release/net7.0/
aws lambda update-function-code --function-name noscore-travis --zip-file fileb://NosCore.Travis.zip > /dev/null;

#serverless noscore-donation
#cd ../../../../../NosCore.DonationLambda/NosCore.Donation
#dotnet lambda deploy-serverless
