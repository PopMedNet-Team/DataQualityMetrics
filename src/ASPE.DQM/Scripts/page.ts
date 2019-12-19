/** This is going to be a common script file for the layout template **/
import Vue from 'vue';
import { Component, Prop } from 'vue-property-decorator';
import '@progress/kendo-ui';
import '@progress/kendo-theme-default/dist/all.css';
import $ from 'jquery';
import { DialogInstaller, Dialog } from '@progress/kendo-dialog-vue-wrapper';
import { ValidatorInstaller, Validator } from '@progress/kendo-validator-vue-wrapper';
import { debug } from 'util';
import axios from 'axios';
import { join } from 'path';
import { Http2ServerResponse } from 'http2';

Vue.use(DialogInstaller);
Vue.use(ValidatorInstaller);

interface UserProfile {
    id: any;
    firstname: string | null;
    lastname: string | null;
    username: string | null;
    email: string | null;
    phonenumber: string | null;
    organization: string | null;
    canAuthorMetric: boolean;
    canSubmitMeasures: boolean;
    isSystemAdministrator: boolean;
}

@Component({
    template: "#vue-login-template"
})
export class LoginDialog extends Vue {    
    username: string = "";    
    password: string = "";    
    errorMessage: string = "";
    @Prop({ default: null })
    returnUrl!: string;

    loginButtonText: string = '<i class="fas fa-sign-in-alt"></i> Login';
    cancelButtonText: string = '<i class="fas fa-times"></i> Cancel';

    public onShowLogin(e) {
        //TODO:disable the login button
        let d = (<any>this.$refs.loginDialog).kendoWidget();
        d.open();

        (<any>this.$refs.txt_username).focus();
    }

    onLogin(e) {

        let validator = (<any>this).kendoValidator;
        if (validator.validate()) {
            axios.post('/api/authentication', {
                username: this.username,
                password: this.password
            }).then((response) => {
                if (this.returnUrl) {
                    window.location.href = this.returnUrl;
                } else {
                    window.location.reload(true);
                }
                return true;
            })
                .catch((error) => {
                    let errors = error.response.data["error"][0];
                    this.errorMessage = errors;
                });
        }

        return false;
    }

    mounted() {
        let validator = (<any>this).kendoValidator;
        validator.options.validateOnBlur = false;
    }

    onCancelLogin(e) {
        this.username = '';
        this.password = '';
        this.errorMessage = '';

        let validator = (<any>this).kendoValidator;
        validator.hideMessages();

        return true;
    }
}

@Component({
    template: "#vue-authenticated-template"
})
export class UserDialog extends Vue {
    profile: UserProfile = {
        id: null,
        firstname: null,
        lastname: null,
        username: null,
        email: null,
        phonenumber: null,
        organization: null,
        canAuthorMetric: false,
        canSubmitMeasures: false,
        isSystemAdministrator: false
    };

    editButtonText: string = '<i class="fas fa-pencil-alt"></i> Edit Profile';
    closeButtonText: string = '<i class="fas fa-times"></i> Close';

    onShowProfile(e) {
        let d = (<any>this.$refs.profileDialog).kendoWidget();
        d.open();
    }

    onEditProfile(e) {
        window.location.href = "/redirect-to-pmn";
    }

    onLogout(e) {
        axios.post('/api/authentication/signout').then(response => {
            window.location.reload(true);
        });
    }

    beforeMount() {
        axios.get('/api/Authentication/Profile').then((response) => {
            this.profile = response.data;
        }).catch((error) => {

        });
    }
}

let LoginVue = Vue.extend({
    components: { "login-dialog": LoginDialog },
    methods: {
        onTriggerLogin: function (evt) {
            let comp = (<any>this.$refs.login_dialog);
            if (comp) {
                comp.onShowLogin();
            }
        }
    }
});

let UserDetailsVue = Vue.extend({
    components: { "user-dialog": UserDialog },
    methods: {
        onTriggerShowProfile: function (e) {
            let comp = (<any>this.$refs.user_dialog);
            if (comp) {
                comp.onShowProfile();
            }
        },
        onTriggerLogout: function (e) {
            let comp = (<any>this.$refs.user_dialog);
            if (comp) {
                comp.onLogout();
            }
        }
    }
});

document.addEventListener("DOMContentLoaded", function (evt) {
    let login = document.getElementById("vue_login");
    if (login) {
        new LoginVue().$mount(login);
    }

    let loginSidebar = document.getElementById("vue_login_sidebar");
    if (loginSidebar) {
        new LoginVue().$mount(loginSidebar);
    }

    let userDetails = document.getElementById("vue_loggedin");
    if (userDetails) {
        new UserDetailsVue().$mount(userDetails);
    }

    let userDetailsSidebar = document.getElementById("vue_loggedin_sidebar");
    if (userDetailsSidebar) {
        new UserDetailsVue().$mount(userDetailsSidebar);
    }
});


