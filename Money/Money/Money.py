
import sys

from PyQt5.QtWidgets import *
from PyQt5 import uic
from PyQt5.QtGui import *
from PyQt5.QtCore import pyqtSlot,Qt


form_class=uic.loadUiType("Money.ui")[0]

class MyWindow(QDialog,form_class):

    def __init__(self):
        super().__init__()
        self.setupUi(self)
        Check=self.Check
        Check.clicked.connect(self.Check_clicked)
        cancel=self.Cancel
        cancel.clicked.connect(self.Cancel_clicked)

    def Check_clicked(self):
        self.table=QTableWidget(4,3,self)
        self.table.setHorizontalHeaderLabels(["이름","금액","식권사용"])
        self.table.show()

    def Cancel_clicked(self):
        self.teName.Text=""
        self.teMoney.Text=""
        

if __name__ == "__main__":
    app=QApplication(sys.argv)
    myWindow=MyWindow()
    myWindow.show()
    app.exec_()