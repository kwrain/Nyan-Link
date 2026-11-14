"""Original AI-authored chiptune score and sound design for Nyan Link.
No external samples or existing melodies. Deterministic PCM synthesis; keep .meta files.
"""
from pathlib import Path
from array import array
import math
import random
import wave

ROOT = Path(__file__).resolve().parents[1] / 'Assets/Resources/Audio/ConceptPixel'
RATE = 44100
TABLE = 2048
WAVES = {
    'lead': [sum(math.sin(2*math.pi*i/TABLE*k)/k for k in (1,3,5,7))*.62 for i in range(TABLE)],
    'bass': [2/math.pi*math.asin(math.sin(2*math.pi*i/TABLE)) for i in range(TABLE)],
    'bell': [math.sin(2*math.pi*i/TABLE)+.24*math.sin(6*math.pi*i/TABLE) for i in range(TABLE)],
}

def note(buf, start, duration, midi, gain=.15, kind='lead', pan=0, decay=False):
    begin=int(start*RATE); count=min(int(duration*RATE),len(buf)//2-begin)
    freq=440*2**((midi-69)/12); step=freq*TABLE/RATE; samples=WAVES[kind]
    left=math.sqrt((1-pan)*.5);right=math.sqrt((1+pan)*.5)
    for j in range(max(0,count)):
        t=j/max(1,count-1)
        env=min(1,j/(RATE*.008))*min(1,(count-j)/(RATE*.03))
        if decay: env*=math.exp(-t*4)
        value=samples[int(j*step)%TABLE]*gain*env
        buf[(begin+j)*2]+=value*left;buf[(begin+j)*2+1]+=value*right

def drum(buf, start, duration, gain, seed, kick=False):
    rng=random.Random(seed); begin=int(start*RATE); count=min(int(duration*RATE),len(buf)//2-begin)
    for j in range(max(0,count)):
        t=j/RATE
        value=(math.sin(2*math.pi*(70*t+3*(1-math.exp(-t*30)))) if kick else rng.uniform(-1,1))
        value*=gain*math.exp(-t*(22 if kick else 65))
        buf[(begin+j)*2]+=value;buf[(begin+j)*2+1]+=value

def save(name, buf, peak=.72):
    maximum=max(abs(x) for x in buf) or 1; factor=min(1,peak/maximum)
    pcm=array('h',(int(max(-1,min(1,x*factor))*32767) for x in buf))
    with wave.open(str(ROOT/(name+'.wav')),'wb') as out:
        out.setnchannels(2);out.setsampwidth(2);out.setframerate(RATE);out.writeframes(pcm.tobytes())

def music(name,bpm,chords,melody):
    beat=60/bpm;buf=array('f',[0])*(int(32*beat*RATE)*2)
    for bar in range(8):
        root,third,fifth=chords[bar//2];base=bar*4*beat
        for i in range(8):
            pitch=(root+12,third+12,fifth+12,third+12)[i%4]
            note(buf,base+i*.5*beat,.42*beat,pitch,.055,'bell',-.45,True)
        for i in range(4):
            note(buf,base+i*beat,.72*beat,root-12 if i%2==0 else fifth-12,.13,'bass',0)
            drum(buf,base+i*beat,.10,.055 if i%2==0 else .025,bar*8+i,i%2==0)
            drum(buf,base+(i+.5)*beat,.045,.024,500+bar*8+i)
        for i,pitch in enumerate(melody[bar]):
            if pitch:note(buf,base+i*.5*beat,.42*beat,pitch,.12,'lead',.25)
    save(name,buf,.66)

def cue(name,notes,duration=.11,kind='bell'):
    buf=array('f',[0])*(int((len(notes)*duration+.15)*RATE)*2)
    for i,pitch in enumerate(notes):
        if pitch:note(buf,i*duration,duration*1.3,pitch,.36,kind,0,True)
    save(name,buf)

def generate():
    ROOT.mkdir(parents=True,exist_ok=True)
    music('adventure',108,[(60,64,67),(57,60,64),(53,57,60),(55,59,62)], [
      [76,0,79,81,79,76,74,0],[72,76,79,0,76,74,72,0],
      [76,79,81,0,84,81,79,76],[72,0,76,79,76,72,71,0],
      [77,0,81,84,81,79,77,0],[76,77,79,0,77,76,72,0],
      [74,0,79,83,81,79,76,74],[71,74,79,0,76,74,72,0]])
    music('boss',140,[(50,53,57),(46,50,53),(43,46,50),(45,49,52)],[
      [74,77,81,77,74,0,72,74],[77,81,84,81,77,74,72,0],
      [74,77,82,77,74,72,70,0],[70,74,77,82,81,77,74,0],
      [74,79,82,79,77,74,70,0],[67,70,74,79,77,74,72,0],
      [73,76,81,76,73,0,72,73],[76,81,85,81,79,76,73,0]])
    cue('match-short',[76,79],.045)
    cue('match-middle',[76,79,84],.055)
    cue('match-long',[76,79,84,88],.065)
    cue('item',[79,84,88,91],.07)
    cue('ui',[84],.035)
    cue('clear',[72,76,79,84,0,79,84,88],.14)
    cue('lose',[72,69,65,60],.18,'bass')
    hit=array('f',[0])*(int(.22*RATE)*2);drum(hit,0,.18,.4,20261114);note(hit,0,.17,43,.25,'bass',0,True);save('hit',hit)

if __name__=='__main__':generate()
