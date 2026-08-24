  Create a file .github/workflows/build-ios.yml:  make it as public project then use actions or else we need to pay for them if you use in pvt repo

    name: Build iOS
    
    on:
      push:
        branches: [ main ]
      workflow_dispatch:
    
    jobs:
      build:
        runs-on: macos-15
    
        steps:
        - name: Checkout Code
          uses: actions/checkout@v4

        - name: Setup .NET 10
          uses: actions/setup-dotnet@v4
          with:
            dotnet-version: '10.0.x'

        - name: Install MAUI iOS Workload
          run: dotnet workload install maui-ios

        - name: Restore Dependencies
          run: dotnet restore src/Vyaya/Vyaya.csproj

        - name: Build for iOS Simulator
          run: dotnet build src/Vyaya/Vyaya.csproj -f net10.0-ios -c Release