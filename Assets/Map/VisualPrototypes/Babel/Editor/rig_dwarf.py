"""JanPohl 원본을 보존하고 별도 Humanoid 리그 FBX 생성. Blender 4.5.3 background 전용."""
import bpy, json, os, sys
from mathutils import Vector, Matrix
root=os.path.abspath(os.path.join(os.path.dirname(__file__), '../AssetDraft/Dwarf'))
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(root,'Source/Dwarf.fbx'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in meshes:
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

def xyz(v):return Vector((v[0],-v[2],v[1])) # Unity의 앞 +Z, 위 +Y -> Blender
arm_data=bpy.data.armatures.new('DwarfHumanoid')
arm=bpy.data.objects.new('DwarfRig',arm_data);bpy.context.collection.objects.link(arm)
bpy.context.view_layer.objects.active=arm;arm.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
    b=arm_data.edit_bones.new(name);b.head=xyz(head);b.tail=xyz(tail)
    b.align_roll(Vector((0,-1,0)))
    if parent:b.parent=arm_data.edit_bones[parent]
    return b
bone('Hips',(0,.72,0),(0,.85,0))
bone('Spine',(0,.85,0),(0,1.02,-.015),'Hips')
bone('Chest',(0,1.02,-.015),(0,1.17,-.025),'Spine')
bone('UpperChest',(0,1.17,-.025),(0,1.27,-.03),'Chest')
bone('Neck',(0,1.27,-.03),(0,1.35,-.025),'UpperChest')
bone('Head',(0,1.35,-.025),(0,1.53,-.025),'Neck')
for side,s in [('Left',1),('Right',-1)]:
    bone(side+'Shoulder',(s*.08,1.20,-.04),(s*.235,1.19,-.065),'UpperChest')
    bone(side+'UpperArm',(s*.235,1.19,-.065),(s*.38,1.005,-.095),side+'Shoulder')
    bone(side+'LowerArm',(s*.38,1.005,-.095),(s*.49,.815,-.13),side+'UpperArm')
    bone(side+'Hand',(s*.49,.815,-.13),(s*.52,.725,-.03),side+'LowerArm')
    # 장갑 메시의 지오데식 끝점으로 확인한 손가락 위치. 기존 손 형태를 보존한다.
    fingers={
        'Thumb':[(.54,.785,-.08),(.58,.785,-.035),(.601,.763,-.004),(.588,.749,.006)],
        'Index':[(.468,.718,-.045),(.468,.670,-.021),(.468,.631,-.007),(.468,.600,-.005)],
        'Middle':[(.516,.718,-.035),(.520,.663,-.011),(.522,.624,.006),(.523,.597,.011)],
        'Ring':[(.558,.732,-.025),(.562,.683,-.003),(.565,.642,.015),(.566,.612,.021)],
        'Little':[(.590,.753,-.014),(.605,.710,.005),(.615,.678,.017),(.620,.656,.022)]}
    for digit,points in fingers.items():
        for i,segment in enumerate(['Proximal','Intermediate','Distal']):
            a,b=points[i],points[i+1]
            bone(side+digit+segment,(s*a[0],a[1],a[2]),(s*b[0],b[1],b[2]),side+'Hand' if i==0 else side+digit+['Proximal','Intermediate'][i-1])
    bone(side+'UpperLeg',(s*.15,.72,0),(s*.195,.415,.025),'Hips')
    bone(side+'LowerLeg',(s*.195,.415,.025),(s*.21,.13,-.035),side+'UpperLeg')
    bone(side+'Foot',(s*.21,.13,-.035),(s*.21,.065,.14),side+'LowerLeg')
    bone(side+'Toes',(s*.21,.065,.14),(s*.21,.06,.20),side+'Foot')
bpy.ops.object.mode_set(mode='OBJECT')

def assign(o, v, pairs):
    for name,w in pairs:
        if w<=0:continue
        g=o.vertex_groups.get(name) or o.vertex_groups.new(name=name);g.add([v.index],w,'REPLACE')

stats=[]
for o in meshes:
    if o.name=='Hammer_low':continue
    if o.name in ['Body_low','Gloves_low','Pants_low']:
        bpy.ops.object.select_all(action='DESELECT');o.select_set(True);arm.select_set(True);bpy.context.view_layer.objects.active=arm
        bpy.ops.object.parent_set(type='ARMATURE_AUTO')
        unweighted=sum(not v.groups for v in o.data.vertices)
        if unweighted:raise RuntimeError(f'자동 웨이트 실패 {o.name}: {unweighted}')
        # 몸/바지의 허리 경계를 같은 골반에 묶어 동작 중 틈이 벌어지지 않게 한다.
        for v in o.data.vertices:
            if (o.name=='Body_low' and v.co.z<.88 and abs(v.co.x)<.27) or (o.name=='Pants_low' and v.co.z>.70):
                for g in o.vertex_groups:g.remove([v.index])
                assign(o,v,[('Hips',1)])
    else:
        o.parent=arm
        mod=o.modifiers.new('Dwarf Skin','ARMATURE');mod.object=arm
        for v in o.data.vertices:
            p=v.co; x,z=p.x,p.z; side='Left' if x>0 else 'Right'
            if o.name.startswith(('Braid','Hair','Eyes','Goggles')):pairs=[('Head',1)]
            elif o.name in ['ShoulderPlate_low','Fur_low']:pairs=[(side+'Shoulder',1)]
            elif o.name=='Sherpe_low':
                t=max(0,min(1,(z-1.02)/.15));pairs=[('Chest',1-t),('UpperChest',t)]
            elif o.name.startswith('Shoe'):
                t=max(0,min(1,(z-.18)/.15));pairs=[(side+'Foot',1-t),(side+'LowerLeg',t)]
            elif o.name.startswith('Glove'):
                pairs=[(side+'LowerArm',1)]
            elif o.name=='Apron_low':
                t=max(0,min(.65,(.64-z)*2.8));pairs=[('Hips',1-t),(side+'UpperLeg',t)]
            else:pairs=[('Hips',1)]
            assign(o,v,pairs)
    stats.append({'object':o.name,'vertices':len(o.data.vertices),'unweighted':sum(not v.groups for v in o.data.vertices)})

# Humanoid 바인드를 위한 T 자세. 모델을 납작하게 스케일하지 않고 관절 회전만 적용한다.
bpy.context.view_layer.objects.active=arm;bpy.ops.object.mode_set(mode='POSE')
for side,s in [('Left',1),('Right',-1)]:
    for part in ['UpperArm','LowerArm','Hand']:
        pb=arm.pose.bones[side+part]
        bpy.context.view_layer.update()
        delta=(pb.tail-pb.head).normalized().rotation_difference(Vector((s,0,0)))
        mat=pb.matrix.copy();mat=Matrix.Translation(pb.head)@delta.to_matrix().to_4x4()@Matrix.Translation(-pb.head)@mat
        pb.matrix=mat
bpy.ops.object.mode_set(mode='OBJECT');bpy.context.view_layer.update()
for o in meshes:
    if o.name=='Hammer_low':continue
    bpy.context.view_layer.objects.active=o
    for mod in list(o.modifiers):
        if mod.type=='ARMATURE':bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.context.view_layer.objects.active=arm;bpy.ops.object.mode_set(mode='POSE');bpy.ops.pose.armature_apply(selected=False);bpy.ops.object.mode_set(mode='OBJECT')
for o in meshes:
    if o.name=='Hammer_low':continue
    mod=o.modifiers.new('Dwarf Skin','ARMATURE');mod.object=arm
    # 각 정점은 상위 4개 영향으로 정규화하여 Unity 기본 스키닝과 맞춘다.
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    bpy.ops.object.vertex_group_limit_total(limit=4);bpy.ops.object.vertex_group_normalize_all(lock_active=False)
os.makedirs(os.path.join(root,'Rigged'),exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=os.path.join(root,'Rigged/Dwarf_Humanoid.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',path_mode='STRIP')
with open(os.path.join(root,'Rigged/rig-report.json'),'w',encoding='utf-8') as f:json.dump({'generator':'rig_dwarf.py','source_has_rig':False,'bones':len(arm.data.bones),'meshes':stats},f,indent=2)
print('DWARF_RIG_OK',len(arm.data.bones),len(stats))

# Substance의 Metallic/Roughness -> URP Metallic(R)/Smoothness(A). 원본 PNG는 보존한다.
import numpy as np
for filename in os.listdir(os.path.join(root,'Textures')):
    if not filename.endswith('_Metallic.png'):continue
    prefix=filename[:-len('_Metallic.png')]
    channels=[]
    for suffix in ['_Metallic.png','_Roughness.png']:
        image=bpy.data.images.load(os.path.join(root,'Textures',prefix+suffix),check_existing=False)
        image.colorspace_settings.name='Non-Color'; image.scale(1024,1024)
        pixels=np.empty(1024*1024*4,dtype=np.float32);image.pixels.foreach_get(pixels)
        channels.append(pixels.reshape(-1,4)[:,0].copy());bpy.data.images.remove(image)
    packed=np.zeros((1024*1024,4),dtype=np.float32);packed[:,0]=channels[0];packed[:,3]=1-channels[1]
    image=bpy.data.images.new(prefix+'_MetallicSmoothness',width=1024,height=1024,alpha=True)
    image.colorspace_settings.name='Non-Color';image.pixels.foreach_set(packed.ravel())
    image.filepath_raw=os.path.join(root,'Textures',prefix+'_MetallicSmoothness.png');image.file_format='PNG';image.save();bpy.data.images.remove(image)
print('DWARF_PBR_PACK_OK')
