using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpaceShooter
{
    public partial class Loginform : Form
    {
        public Loginform()
        {
            InitializeComponent();
            this.AcceptButton = loginBtn; // Enter키를 누르면 loginBtn 클릭
        }

        private void Loginform_Load(object sender, EventArgs e)
        {

        }

        private void loginBtn_Click(object sender, EventArgs e)
        {
            // 텍스트 박스에 적은 값 가져와서 id, pw 변수에 저장
            string id = idTb.Text.Trim();
            string pw = pwTb.Text.Trim();

            // id나 pw가 비어있으면 경고 메시지 출력하고 포커스 설정
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
            {
                MessageBox.Show("아이디와 비밀번호를 모두 입력해주세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (string.IsNullOrEmpty(id))
                {
                    idTb.Focus();
                }
                else
                {
                    pwTb.Focus();
                }

                return;
            }



        }
    }
}
