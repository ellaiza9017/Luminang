import re
import os

manager_path = r"C:\Users\Irah\Documents\Unity Projects\Luminang_New\Assets\Scripts\UI\Player Customization\ShopManager.cs"
frame_path = r"C:\Users\Irah\Documents\Unity Projects\Luminang_New\Assets\Scripts\UI\Player Customization\ShopFrameUI.cs"

with open(manager_path, "r", encoding="utf-8") as f:
    content = f.read()

# The class ShopFrameUI is at the bottom of the file. 
# We'll match from 'public class ShopFrameUI' to the end of the file.
pattern = r"public class ShopFrameUI : MonoBehaviour.*"
match = re.search(pattern, content, flags=re.DOTALL)

if match:
    class_code = match.group(0)
    
    # Remove from ShopManager.cs
    new_content = content[:match.start()].strip() + "\n"
    with open(manager_path, "w", encoding="utf-8") as f:
        f.write(new_content)
        
    # Write to ShopFrameUI.cs
    imports = "using UnityEngine;\nusing UnityEngine.UI;\nusing TMPro;\nusing System.Collections.Generic;\n\n"
    with open(frame_path, "w", encoding="utf-8") as f:
        f.write(imports + class_code)
