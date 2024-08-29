import { Component, OnInit } from '@angular/core';
import { AuthService } from './identity/service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  public isSignedIn: boolean = false;
  sharedKey = 'UYCMCQ3D73S3ZOWJWLZAUSF22N3P7OAV';

  constructor(private auth: AuthService, private router: Router) { }

  ngOnInit() {
    this.auth.onStateChanged().forEach((state: any) => {
      this.auth.isSignedIn().forEach((signedIn: boolean) => this.isSignedIn = signedIn);
    });
  }

  generateOtpAuthUrl(secret: string, issuer: string, accountName: string): string {
    return `otpauth://totp/${issuer}:${accountName}?secret=${secret}&issuer=${issuer}`;
  }

  signOut() {
    if (this.isSignedIn) {
      this.auth.signOut().forEach(response => {
        if (response) {
          this.router.navigateByUrl('');
        }
      });
    }
  }

  title = 'ngidentity.client';
}
