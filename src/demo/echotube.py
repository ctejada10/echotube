#! /usr/bin/env python3
"""
	Plot and identification code for touches along a rubber tube using
	EchoTube
"""
import asyncio
import sys
from collections import deque
from time import time

import numpy as np
import pyqtgraph as pg
import pyqtgraph.console
import serial_asyncio
from cobs import cobs
from quamash import QEventLoop, QtCore, QtGui

from scipy.signal import hilbert
from scipy.signal import butter, filtfilt, freqz


class EchoTube(pg.GraphicsWindow):
	
	def __init__(self, serial, printHz=False, bufsize=2048, baudrate=1e6):
		super().__init__()

		self.serial   = serial
		self.baudrate = baudrate
		self.bufsize  = bufsize
		self.run      = True
		self.fs       = 80000
		self.c        = 34300

		# Filter parameters
		self.alpha  = 0.05
		self.cutoff = 100
		self.order  = 5

		# Touch detection parameters
		self.threshold = 180.0

		# Creating plot window
		app  = QtGui.QApplication(sys.argv)
		loop = QEventLoop(app)
		asyncio.set_event_loop(loop)
		self.resize(800, 400)

		# App ranges
		self.ranges = {'a': (20, 53), 'b': (62, 101), 'c': (110, 150)}


		# Plotting parameters
		p             = self.addPlot()
		self.plotline = p.plot(pen='y')
		self.baseline = p.plot(pen='b')
		self.l        = pg.TextItem()
		self.l.setText('')
		self.l.setColor('y')
		self.l.setTextWidth(500)
		p.addItem(self.l)
		self.show()

		loop.run_until_complete(self.read_data())


	async def read_data(self):
		reader, _ = await serial_asyncio.open_serial_connection(
			url=self.serial, baudrate=self.baudrate)
		while self.run:
			buf = await reader.readuntil(b'\x00')
			try:
				data = list(cobs.decode(buf[:-1])) # Discard separator
			except cobs.DecodeError as e:
				sys.stderr.write(str(e) + '\n')
				continue
			else:
				data = np.array(data[2:])

				# Plot
				self.plotline.setData(data)
				peaks = np.where(data[150:] > self.threshold)[0]
				if len(peaks):
					peak = 150 + (peaks[0] - 10)
					distance = (self.c * (peak / self.fs)) / 2 - 10
					text = ''
					if distance > self.ranges['a'][0] and distance < self.ranges['a'][1]:
						text = 'a'
					elif distance > self.ranges['b'][0] and distance < self.ranges['b'][1]:
						text = 'b'
					elif distance > self.ranges['c'][0] and distance < self.ranges['c'][1]:
						text = 'c'
					txt = '<div style="text-align: center"><span style="color: #FFF;font-size:28pt;">%s</span></div>' % (text)
					self.l.setHtml(txt)
				else:
					self.l.setText('')

	

	def keyPressedEvent(self, ev):
		event = ev.text()
		print(event)
		if ev.key() == 16777216:
			self.run = False
	

	def filter(self, signal):
		w = self.cutoff / (self.fs / 2)
		b, a = butter(5, w, 'low')
		filtered = filtfilt(b, a, signal)

		return filtered


	def get_distance(self, peaks):
		return (self.c * (peaks[0] / self.sampling_rate)) / 2


	def get_peaks(self, signal):
		return np.where(signal >= self.threshold)[0]



if __name__ == '__main__':
	from clize import run
	run(EchoTube)
