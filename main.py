import os
import openai

# Set your OpenAI key via environment variable
openai.api_key = os.getenv("OPENAI_API_KEY")

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
    full_prompt = template.replace("{{user_prompt}}", user_prompt).replace("{{metadata_yaml}}", metadata_yaml)

    print("\n⏳ Generating plugin code via OpenAI...\n")

    response = openai.ChatCompletion.create(
        model="gpt-4o",
        messages=[
            {"role": "system", "content": "You are a senior Dynamics 365 plugin developer."},
            {"role": "user", "content": full_prompt}
        ],
        temperature=0.3
    )

    plugin_code = response["choices"][0]["message"]["content"]
    print("✅ Generated plugin code:\n")
    print(plugin_code)

if __name__ == "__main__":
    main()
