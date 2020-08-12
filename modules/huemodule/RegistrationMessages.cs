namespace huemodule {

    internal class RegistrationRequest {

        public RegistrationRequest(string leafDeviceId) {
            LeafDeviceId = leafDeviceId;
        }

        public string LeafDeviceId { get; }
        
    }

    internal class RegistrationResponse {

        public string LeafDeviceId { get; set; }

    }

}