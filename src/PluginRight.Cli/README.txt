# PluginRight.CLI

# build
dotnet build ./PluginRight.CLI -c Release

# generate to stdout (quiet logs)
./bin/Release/net8.0/PluginRight.CLI generate \
  --spec ./examples/account.create.task.spec.json \
  --templates ./templates \
  --template StandardPlugin.cs.txt \
  --seed 0 --quiet > Plugin.cs

# or write directly to a file (stdout stays empty; logs to stderr)
./bin/Release/net8.0/PluginRight.CLI generate \
  --spec ./examples/account.create.task.spec.json \
  --templates ./templates \
  --template StandardPlugin.cs.txt \
  --seed 0 --out ./out/Plugin.cs