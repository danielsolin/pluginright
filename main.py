from openai import OpenAI

# Load OpenAI API key from a separate file

def load_api_key():
    # Read first non-empty, non-comment line from api_key.txt
    try:
        with open("api_key.txt", "r", encoding="utf-8") as f:
            for raw in f:
                line = raw.strip()
                if not line or line.startswith("#"):
                    continue
                if "your-api-key-here" in line:
                    raise RuntimeError(
                        "api_key.txt contains a placeholder; replace it with "
                        "your real API key."
                    )
                return line
    except FileNotFoundError:
        pass

    raise RuntimeError(
        "OpenAI API key not found. Create api_key.txt with your key on the "
        "first non-comment line."
    )

client = OpenAI(api_key=load_api_key())

def load_template(path):
    with open(path, "r", encoding="utf-8") as f:
        return f.read()

def main():
    user_prompt = input("Describe what the plugin should do:\n> ")

    # Example hardcoded metadata for now
    metadata_yaml = """
entities:
  - name: contact
    fields:
      - emailaddress1 (string)
      - firstname (string)
      - parentcustomerid (lookup: account)
  - name: account
    fields:
      - emailaddress1 (string)
"""

    template = load_template("prompt_template.txt")
    full_prompt = template.replace("{{user_prompt}}", user_prompt)
    full_prompt = full_prompt.replace("{{metadata_yaml}}", metadata_yaml)

    print("\n⏳ Generating plugin code via OpenAI...\n")

    response = client.chat.completions.create(
        model="gpt-4o",
        messages=[
            {
                "role": "system",
                "content": (
                    "You are a senior Dynamics 365 plugin developer."
                ),
            },
            {"role": "user", "content": full_prompt},
        ],
        temperature=0.3,
    )

    plugin_code = response.choices[0].message.content
    print("✅ Generated plugin code:\n")
    print(plugin_code)

if __name__ == "__main__":
    main()