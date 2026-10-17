"""Generate original close-up stereo ASMR foley. No downloaded recordings."""
import argparse, math, random, struct, wave, json
from pathlib import Path
parser=argparse.ArgumentParser()
parser.add_argument('--output',type=Path,default=Path(__file__).resolve().parents[1]/'Assets'/'EggLab'/'Audio')
args=parser.parse_args();out=args.output;out.mkdir(parents=True,exist_ok=True)
rate=44100
specs={'tap':(.20,3),'crack':(.32,1),'break':(.85,1),'squish':(.66,3),'release':(.56,3),'pour':(1.65,1),'liquid-loop':(2.8,1),'bubble':(.24,4)}
report=[]
for kind,(duration,count) in specs.items():
 for variation in range(count):
  name=kind if variation==0 else kind+'-'+str(variation+1).zfill(2)
  rng=random.Random(7319+sum(ord(c) for c in kind)*19+variation*167)
  length=round(duration*rate)
  common=[rng.uniform(-1,1) for _ in range(length)]
  sides=[[rng.uniform(-1,1) for _ in range(length)] for _ in range(2)]
  # Tiny damped liquid grains replace the old long sine sweeps.
  grain_count={'tap':3,'crack':8,'break':20,'squish':15,'release':10,'pour':32,'liquid-loop':0,'bubble':3}[kind]
  grains=[]
  for k in range(grain_count):
   center=rng.uniform(.01,duration*.85)
   grains.append((center,rng.uniform(95,410),rng.uniform(28,65),rng.uniform(.025,.095),rng.uniform(.012,.045)))
  channels=[]
  for channel in range(2):
   fast=slow=previous=0.;phase=0.;values=[]
   # Two full filter passes make the loop a periodic fixed point.
   passes=2 if kind=='liquid-loop' else 1
   for turn in range(passes):
    values=[]
    for i in range(length):
     t=i/rate;x=t/duration;n=common[i]*.84+sides[channel][i]*.16
     coefficient={'tap':.13,'crack':.18,'break':.14,'squish':.065,'release':.075,'pour':.09,'liquid-loop':.055,'bubble':.055}[kind]
     fast+=coefficient*(n-fast);slow+=.012*(fast-slow)
     texture=fast-slow*.72
     if kind=='tap':env=(1-math.exp(-t*350))*math.exp(-t*(25+variation*2));value=texture*1.6*env
     elif kind=='crack':
      env=sum((.8**k)*math.exp(-((t-(.018+k*.048))/.014)**2) for k in range(5));value=texture*1.35*env
     elif kind=='break':
      env=sum((.87**k)*math.exp(-((t-(.035+k*.047))/.018)**2) for k in range(14));value=texture*1.2*env
     elif kind=='liquid-loop':
      env=.78+.12*math.sin(2*math.pi*x*3)+.07*math.sin(2*math.pi*x*7)
      value=texture*2.0*env + .022*math.sin(2*math.pi*173*x+.5*math.sin(2*math.pi*3*x))
     else:
      env=math.sin(math.pi*x)**(.6 if kind=='pour' else .85)
      value=texture*(2.4 if kind in ('squish','release') else 2)*env
     for center,freq,decay,amp,width in grains:
      age=t-center-(channel*.00035)
      if 0<age<.18:
       e=(1-math.exp(-age/width*5))*math.exp(-age*decay)
       value+=math.sin(2*math.pi*(freq*age-freq*.7*age*age))*e*amp
     if kind!='liquid-loop':
      fade=min(1,t/.012)*min(1,(duration-t)/.045)
      value*=fade
     # A soft first-order low pass removes brittle synthetic transients.
     previous+=.30*(value-previous);values.append(previous)
   channels.append(values)
  mean=[sum(c)/length for c in channels]
  for c in range(2):channels[c]=[v-mean[c] for v in channels[c]]
  if kind!='liquid-loop':
   for c in range(2):
    channels[c]=[v*min(1,i/rate/.012)*min(1,(length-1-i)/rate/.045) for i,v in enumerate(channels[c])]
  peak=max(max(abs(v) for v in c) for c in channels)
  rms=math.sqrt(sum(v*v for c in channels for v in c)/(length*2))
  target=.10 if kind in ('tap','crack','break') else .14
  peak_limit=.40 if kind=='liquid-loop' else .48
  gain=min(target/max(rms,1e-9),peak_limit/max(peak,1e-9))
  with wave.open(str(out/(name+'.wav')),'wb') as w:
   w.setnchannels(2);w.setsampwidth(2);w.setframerate(rate)
   frames=bytearray()
   for i in range(length):
    frames.extend(struct.pack('<hh',round(channels[0][i]*gain*32767),round(channels[1][i]*gain*32767)))
   w.writeframes(frames)
  entry={'clip':name,'duration':duration,'channels':2,'peak':peak*gain,'rms':rms*gain,'bytes':len(frames)}
  report.append(entry);print(name,round(entry['peak'],3),round(entry['rms'],3))
(out/'ASMR-audio-manifest.json').write_text(json.dumps({'version':'ASMR-1','origin':'Locally synthesized original foley; no third-party recordings','sampleRate':rate,'clips':report},indent=2))
