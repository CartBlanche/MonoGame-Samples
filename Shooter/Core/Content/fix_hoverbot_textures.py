import bpy
import os

# Clear existing scene
bpy.ops.wm.read_factory_settings(use_empty=True)

# Import the HoverBot FBX
fbx_input = r"C:\Users\savag\source\repos\CartBlanche\MonoGame\MonoGame-Samples\Shooter\Core\Content\Models\HoverBot.fbx"
bpy.ops.import_scene.fbx(filepath=fbx_input)

# Get the texture we want to use
texture_path = r"C:\Users\savag\source\repos\CartBlanche\MonoGame\MonoGame-Samples\Shooter\Core\Content\Textures\Enemies\HoverBot_Body_Albedo.png"

# Fix materials to use the correct texture path
for mat in bpy.data.materials:
    if mat.use_nodes:
        for node in mat.node_tree.nodes:
            if node.type == 'TEX_IMAGE':
                # Update the image path
                if os.path.exists(texture_path):
                    node.image = bpy.data.images.load(texture_path)
                    print(f"Updated texture for material {mat.name}")

# Export as FBX with embedded textures
fbx_output = r"C:\Users\savag\source\repos\CartBlanche\MonoGame\MonoGame-Samples\Shooter\Core\Content\Models\HoverBot_Fixed.fbx"
bpy.ops.export_scene.fbx(
    filepath=fbx_output,
    use_selection=False,
    path_mode='COPY',  # Copy textures relative to FBX
    embed_textures=True,  # Embed textures in FBX
    axis_forward='-Z',
    axis_up='Y'
)

print(f"Successfully exported fixed HoverBot to {fbx_output}")
