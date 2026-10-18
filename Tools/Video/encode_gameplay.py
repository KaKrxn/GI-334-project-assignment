from pathlib import Path
import imageio_ffmpeg,subprocess
r=Path('Art/GameplayVideo/FirstColony').resolve()
import re
count=int(re.search(r'Frames=(\d+)',(r/'Result.txt').read_text()).group(1))
frames=sorted((r/'Frames').glob('*.jpg'))[:count]
chapters=[]
for line in (r/'Chapters.tsv').read_text(encoding='utf-8-sig').splitlines():
 n,t=line.split('\t',1);chapters.append((int(n),t))
def ts(s):
 h=int(s//3600);m=int(s%3600//60);sec=s%60
 return f'{h}:{m:02}:{sec:05.2f}'
header='''[Script Info]
ScriptType: v4.00+
PlayResX: 1280
PlayResY: 800
WrapStyle: 0

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Default,Tahoma,28,&H00F0E8D8,&H000000FF,&H0015100B,&H0015100B,0,0,0,0,100,100,0,0,1,1,0,2,32,32,16,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
'''
lines=[]
for i,(start,text) in enumerate(chapters):
 end=chapters[i+1][0] if i+1<len(chapters) else len(frames)
 lines.append(f'Dialogue: 0,{ts(start/4)},{ts(end/4)},Default,,0,0,0,,{text}')
(r/'Tutorial.ass').write_text(header+'\n'.join(lines),encoding='utf-8-sig')
ff=imageio_ffmpeg.get_ffmpeg_exe()
cmd=[ff,'-hide_banner','-loglevel','warning','-y','-framerate','4','-i','Frames/%05d.jpg','-vf',"pad=1280:800:0:0:color=0x15100b,subtitles=Tutorial.ass",'-frames:v',str(count*6),'-r','24','-c:v','libx264','-preset','medium','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart','First_Colony_Tutorial_TH.mp4']
subprocess.run(cmd,cwd=r,check=True)
print(str(r/'First_Colony_Tutorial_TH.mp4'))
print('Frames:',len(frames),'Duration:',len(frames)/4)
