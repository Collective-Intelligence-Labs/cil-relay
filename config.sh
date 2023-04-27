
# Intall brew

/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
touch ~/.zshrc
export PATH=/opt/homebrew/bin:$PATH
source ~/.zshrc

# Install protobuf

brew install protobuf

## complie proto files for C#
# protoc --proto_path=Proto --csharp_out=Scheme --csharp_opt=file_extension=.g.cs  Proto/event.proto 
# protoc --proto_path=Proto --csharp_out=Scheme --csharp_opt=file_extension=.g.cs  Proto/operation.proto
# protoc --proto_path=Proto --csharp_out=Scheme --csharp_opt=file_extension=.g.cs  Proto/command.proto

# Install mongo

brew install mongodb-community@6.0

# Install Kafka

brew install kafka

# Install zookeper

brew install zookeper

# start all the services

brew services start zookeeper

brew services start kafka

brew services start mongodb-community@6.0