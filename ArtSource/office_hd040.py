from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import arabic_reshaper
from bidi.algorithm import get_display
import math,json
T=Path(__file__).resolve().parents[1]/'DiwanVR/Assets/Diwan/Textures';N=T/'Office040';N.mkdir(exist_ok=True)
FONT=r'C:\Windows\Fonts\arial.ttf';BOLD=r'C:\Windows\Fonts\arialbd.ttf'
def ar(t):return get_display(arabic_reshaper.reshape(t),base_dir='R')
def text(d,t,xy,size=40,fill='#18212b',bold=False,anchor='ra'):
    d.text(xy,ar(t),font=ImageFont.truetype(BOLD if bold else FONT,int(size)),fill=fill,anchor=anchor)
S=2
def txt(d,t,xy,size=40,**kw):text(d,t,(xy[0]*S,xy[1]*S),size*S,**kw)
def line(d,xy,fill='#86949e',width=2):d.line(tuple(v*S for v in xy),fill=fill,width=width*S)
paper=Image.new('RGB',(2480,3508),'#faf9f4');paper.save(T/'Paper.png',optimize=True)
im=paper.copy();d=ImageDraw.Draw(im)
txt(d,'وزارة الداخلية',(1125,85),46,bold=True);txt(d,'إدارة التأهيل والتدريب',(1125,155),44,bold=True)
for yy,t in [(300,'العدد  352 / ت'),(365,'التاريخ  2026/11/05'),(485,'من  مدير إدارة التأهيل والتدريب'),(570,'الموضوع  تحديد ساعات العمل')]:txt(d,t,(1125,yy),38,bold=yy==570)
lines=['تحدد ساعات العمل الرسمية في الإدارة','من الساعة الثامنة صباحًا حتى الساعة الثالثة مساءً،','من يوم الأحد إلى يوم الخميس من كل أسبوع،','اعتبارًا من 2026/11/09. يرجى التقيّد بما ورد.','للاطلاع والتنفيذ حسب الأصول.']
for j,t in enumerate(lines):txt(d,t,(1125,740+j*88),40)
txt(d,'مدير إدارة التأهيل والتدريب',(720,1300),38,bold=True)
d.line([(int((210+i*5)*S),int((1400+math.sin(i*.5)*9+math.sin(i*.12)*12)*S)) for i in range(59)],fill='#203659',width=5)
txt(d,'المرفقات  لا يوجد',(1125,1515),32);txt(d,'نسخة تدريبية',(1125,1630),24,fill='#777777');im.save(T/'IncomingLetter.png',optimize=True)
regs=['سجل البرقيات الوارد','سجل الرسائل الواردة','سجل الضبوط','سجل الرسائل الصادرة','دفتر الذمة']
binders=['مصنف نسخ الضبوط','مصنف الرسائل السرية','مصنف الرسائل الواردة','مصنف رؤساء السلك','مصنف تقارير التفتيش','مصنف البرقيات','مصنف الأوامر العامة','مصنف احتياطي']
fields=[['رقم القيد','تاريخ الورود','الجهة المرسلة','رقم البرقية وتاريخها','خلاصة البرقية','ملاحظات'],['رقم القيد','تاريخ الورود','الجهة المرسلة','رقم الكتاب وتاريخه','خلاصة الكتاب','ملاحظات'],['رقم القيد','تاريخ الضبط','الجهة المنظمة','رقم الضبط','خلاصة الضبط','ملاحظات'],['رقم الصادر','تاريخ الصدور','الجهة المرسل إليها','رقم الكتاب وتاريخه','خلاصة الكتاب','ملاحظات'],['التسلسل','تاريخ التسليم','المستلم','رقم المستند','الموضوع','توقيع المستلم']]
for i,name in enumerate(regs):
    im=paper.copy();d=ImageDraw.Draw(im);txt(d,name,(1130,65),40,bold=True);txt(d,'رقم الصفحة  24',(1130,123),26)
    edges=[60,160,345,580,790,1060,1180]
    for x in edges:line(d,(x,220,x,1620))
    for y in [220,330,530,840,1150,1460,1620]:line(d,(60,y,1180,y))
    for l,r,t in zip(edges[:-1],edges[1:],reversed(fields[i])):
        size=25
        while ImageFont.truetype(BOLD,size*S).getlength(ar(t))>(r-l-12)*S:size-=1
        txt(d,t,((l+r)/2,245),size,bold=True,anchor='ma')
    vals=['126','2026/11/07','قسم التدريب','351/ت','تنظيم الاختبارات','لا يوجد'] if i==1 else ['']*6
    for l,r,t in zip(edges[:-1],edges[1:],reversed(vals)):txt(d,t,((l+r)/2,370),20,anchor='ma')
    for y in [620,700,780]:line(d,(60,y,1180,y),fill='#d4dce0',width=1)
    im.save(T/f'RegisterPage{i}.png',optimize=True)
    if i==1:im.save(T/'RegisterPage.png',optimize=True)
def label(name,title,w=2048,h=512,bg='#eeeadd'):
    im=Image.new('RGB',(w,h),bg);d=ImageDraw.Draw(im);d.rounded_rectangle((20,20,w-20,h-20),12,outline='#6e6759',width=4);size=int(h*.30)
    while ImageFont.truetype(BOLD,size).getlength(ar(title))>w-95:size-=1
    text(d,title,(w/2,h/2),size,bold=True,anchor='mm');im.save(T/(name+'.png'),optimize=True)
for i,n in enumerate(regs):label('RegisterLabel'+str(i),n)
for i,n in enumerate(binders):label('BinderLabel'+str(i),n)
label('DepartmentSign','إدارة التأهيل والتدريب',3072,512);label('DoorSign','الديوان',2048,768)
label('ReceiptSlip','تاريخ الورود 2026/11/08',2048,512)
im=paper.copy();d=ImageDraw.Draw(im);txt(d,'قيد 126',(1125,80),36);txt(d,'الموضوع  تنظيم الاختبارات',(1125,220),40,bold=True);txt(d,'يحفظ الكتاب حسب الأصول.',(1125,420),38);im.save(T/'PreviousLetter.png',optimize=True)
# Native vector redraw of the book atlas tiles; identical UV regions to 0.2.
titles=['أصول العمل الإداري','المراسلات الرسمية','إدارة الوثائق','تنظيم أعمال الديوان','التأهيل والتدريب','الدليل الإداري','حفظ السجلات','شؤون العاملين','إجراءات التفتيش','إدارة الوقت','تنظيم الأرشيف','القرارات والتعاميم']
colors=[(43,59,56),(67,33,31),(41,52,74),(89,74,46),(69,74,64),(46,39,31)]
for i,t in enumerate(titles):
    color=colors[i%6];gold='#c4b38a';cover=Image.new('RGB',(1024,1536),color);d=ImageDraw.Draw(cover);d.rectangle((50,60,974,1476),outline=gold,width=5);d.rectangle((68,78,956,1458),outline=gold,width=2)
    words=t.split();rows=[' '.join(words[:2]),' '.join(words[2:])]
    for j,row in enumerate(rows):text(d,row,(512,580+j*120),76,fill=gold,bold=True,anchor='mm')
    text(d,'مرجع إداري',(512,1200),48,fill=gold,anchor='mm');cover.save(T/f'BookCover{i}.png',optimize=True)
    spine=Image.new('RGB',(512,2048),color);d=ImageDraw.Draw(spine)
    for y in [90,115,1925,1950]:d.line((35,y,477,y),fill=gold,width=4)
    for j,word in enumerate(words):text(d,word,(256,650+j*150),70,fill=gold,bold=True,anchor='mm')
    spine.save(T/f'BookSpine{i}.png',optimize=True)
    atlas=Image.new('RGB',(2048,2048),color);atlas.paste(cover,(0,512));atlas.paste(spine,(1024,0));atlas.paste(Image.open(T/'PaperEdgesClean.png').resize((512,2048),Image.Resampling.LANCZOS),(1536,0));atlas.save(T/f'ReferenceAtlas{i}.png',optimize=True)
im=Image.new('RGBA',(1024,576),(255,255,255,0));d=ImageDraw.Draw(im);blue='#243f88';d.rounded_rectangle((25,25,998,549),55,outline=blue,width=16);d.rounded_rectangle((43,43,980,531),42,outline=blue,width=5)
text(d,'ديوان إدارة التأهيل والتدريب',(512,180),60,fill=blue,bold=True,anchor='mm');text(d,'وارد',(512,302),76,fill=blue,bold=True,anchor='mm');text(d,'التاريخ:      /      /',(512,422),49,fill=blue,anchor='mm');im.save(N/'StampImprint.png',optimize=True)
im=Image.new('RGB',(1024,512),'#e8dfc7');d=ImageDraw.Draw(im);text(d,'ختم الديوان',(512,256),132,bold=True,anchor='mm');im.save(N/'StampLabel.png',optimize=True)
im=Image.new('RGB',(1024,352),'#a8b99d');d=ImageDraw.Draw(im);text(d,'الديوان',(920,45),66,anchor='ra');d.text((65,125),'08:00',font=ImageFont.truetype(FONT,134),fill='#344334');d.text((680,250),'101',font=ImageFont.truetype(FONT,52),fill='#344334');im.save(N/'PhoneLCD.png',optimize=True)
im=Image.new('RGBA',(768,832),(255,255,255,0));d=ImageDraw.Draw(im)
for row,chars in enumerate(['123','456','789','*0#']):
    for col,ch in enumerate(chars):d.text(((col+.5)*256,(row+.5)*208),ch,font=ImageFont.truetype(BOLD,112),fill='#23302f',anchor='mm')
im.save(N/'TelephoneKeys.png',optimize=True)
# Fully opaque key legend layer: match the light keycaps between the ink marks.
bg=Image.new('RGB',im.size,'#b0b7b4');bg.paste(im,mask=im.getchannel('A'));bg.save(N/'TelephoneKeys.png',optimize=True)
manifest={p.name:list(Image.open(p).size) for p in [T/'IncomingLetter.png',T/'RegisterPage1.png',T/'DepartmentSign.png',T/'ReferenceAtlas0.png']};(T.parents[2]/'Validation/hd040_assets.json').write_text(json.dumps({'nativeRedraw':True,'images':manifest},indent=2))
print('Native high-resolution paper, labels, book atlases and office artwork regenerated.')
